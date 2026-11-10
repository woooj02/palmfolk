using System;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    // D9 (MR3, MR5): which mode runs and where the valley starts, headless.
    public class ModeFlowTests
    {
        private static readonly ValleyPose Saved = new ValleyPose(new Vec3(0.2, 0.74, -0.3), new Vec3(1, 0, 1), 1.15);
        private static SaveData Save(ValleyMode mode, string anchor = null, ValleyPose pose = null) => new SaveData { Mode = mode, AnchorId = anchor, Pose = pose };

        [Test] public void AFreshTableLaunchPlants()
        {
            var flow = new ModeFlow(new SaveData(), true);
            Assert.That(flow.Chosen, Is.EqualTo(ValleyMode.Table)); Assert.That(flow.Running, Is.EqualTo(ValleyMode.Table));
            Assert.That(flow.Passthrough, Is.True); Assert.That(flow.Decided, Is.True); Assert.That(flow.StartPose, Is.Null);
            Assert.That(flow.Search, Is.EqualTo(AnchorSearch.None)); Assert.That(new ModeFlow(null, true).Decided, Is.True);
        }

        [Test] public void AFoundAnchorOpensThereAtTheSavedScale()
        {
            var flow = new ModeFlow(Save(ValleyMode.Table, "a1", Saved), true);
            Assert.That(flow.Decided, Is.False); Assert.That(flow.Search, Is.EqualTo(AnchorSearch.Finding));
            flow.Wait(4.9); Assert.That(flow.Decided, Is.False); Assert.That(flow.FindProgress, Is.EqualTo(0.98).Within(1e-9));
            Assert.That(flow.AnchorFound(new Vec3(0.21, 0.75, -0.29), new Vec3(0, 0.3, 2)), Is.True);
            Assert.That(flow.Decided, Is.True); Assert.That(flow.Search, Is.EqualTo(AnchorSearch.Found));
            Assert.That(flow.StartPose.Origin, Is.EqualTo(new Vec3(0.21, 0.75, -0.29)));
            Assert.That(flow.StartPose.Forward.Z, Is.EqualTo(1).Within(1e-12)); Assert.That(flow.StartPose.Scale, Is.EqualTo(1.15));
            flow.Wait(10); Assert.That(flow.Search, Is.EqualTo(AnchorSearch.Found), "a found anchor stays found");
            Assert.That(flow.AnchorId, Is.EqualTo("a1"));
        }

        [Test] public void NoAnchorWithinFiveSecondsMeansReplant()
        {
            var flow = new ModeFlow(Save(ValleyMode.Table, "a1", Saved), true);
            for (int i = 0; i < 299; i++) flow.Wait(1 / 60.0);
            Assert.That(flow.Decided, Is.False);
            flow.Wait(1 / 60.0 + 1e-9);
            Assert.That(flow.Decided, Is.True); Assert.That(flow.Search, Is.EqualTo(AnchorSearch.Lost)); Assert.That(flow.StartPose, Is.Null);
            Assert.That(flow.AnchorFound(Vec3.Zero, new Vec3(0, 0, 1)), Is.False, "too late");
            Assert.That(flow.StartPose, Is.Null); Assert.That(flow.Running, Is.EqualTo(ValleyMode.Table));
            flow.Wait(double.NaN); flow.Wait(-3);

            var missing = new ModeFlow(Save(ValleyMode.Table, "gone", Saved), true);
            missing.AnchorMissing(); Assert.That(missing.Search, Is.EqualTo(AnchorSearch.Lost)); Assert.That(missing.Decided, Is.True);
            var broken = new ModeFlow(Save(ValleyMode.Table, "bad", Saved), true);
            Assert.That(broken.AnchorFound(new Vec3(double.NaN, 0, 0), new Vec3(0, 0, 1)), Is.False); Assert.That(broken.Search, Is.EqualTo(AnchorSearch.Lost));
            var upright = new ModeFlow(Save(ValleyMode.Table, "up", Saved), true);
            Assert.That(upright.AnchorFound(Vec3.Zero, new Vec3(0, 1, 0)), Is.False, "an anchor facing straight up has no forward");
        }

        [Test] public void WithoutPassthroughTableRunsAsVrAndKeepsTheChoice()
        {
            var flow = new ModeFlow(Save(ValleyMode.Table, "a1", Saved), false);
            Assert.That(flow.Running, Is.EqualTo(ValleyMode.VR)); Assert.That(flow.FellBack, Is.True); Assert.That(flow.Passthrough, Is.False);
            Assert.That(flow.Decided, Is.True); Assert.That(flow.StartPose, Is.SameAs(Saved)); Assert.That(flow.Search, Is.EqualTo(AnchorSearch.None));
            var save = flow.Into(new SaveData());
            Assert.That(save.Mode, Is.EqualTo(ValleyMode.Table)); Assert.That(save.AnchorId, Is.EqualTo("a1"), "the anchor stays for a launch with passthrough");
            Assert.That(new ModeFlow(Save(ValleyMode.Table), false).StartPose, Is.Null, "nothing saved: plant in VR");
        }

        [Test] public void VrResumesAtTheSavedPose()
        {
            var flow = new ModeFlow(Save(ValleyMode.VR, null, Saved), true);
            Assert.That(flow.Running, Is.EqualTo(ValleyMode.VR)); Assert.That(flow.FellBack, Is.False); Assert.That(flow.Passthrough, Is.False);
            Assert.That(flow.StartPose, Is.SameAs(Saved));
        }

        [Test] public void PlantingReplacesTheAnchor()
        {
            var flow = new ModeFlow(Save(ValleyMode.Table, "old", Saved), true);
            flow.AnchorMissing();
            Assert.That(flow.Replace(), Is.EqualTo("old")); Assert.That(flow.AnchorId, Is.Null);
            Assert.That(flow.Into(new SaveData()).AnchorId, Is.Null, "a save before the new anchor is stored asks to replant next time");
            flow.Anchored("new"); Assert.That(flow.AnchorId, Is.EqualTo("new"));
            Assert.Throws<ArgumentException>(() => flow.Anchored("")); Assert.Throws<ArgumentException>(() => flow.Anchored(new string('a', 129)));
        }

        [Test] public void TheWaystoneSwitchesModesAndLosingPassthroughFallsBack()
        {
            var flow = new ModeFlow(new SaveData(), true);
            Assert.That(flow.Choose(ValleyMode.VR, true), Is.True); Assert.That(flow.Running, Is.EqualTo(ValleyMode.VR)); Assert.That(flow.FellBack, Is.False);
            Assert.That(flow.Choose(ValleyMode.Table, false), Is.False, "no passthrough: still VR");
            Assert.That(flow.FellBack, Is.True); Assert.That(flow.Chosen, Is.EqualTo(ValleyMode.Table));
            Assert.That(flow.Choose(ValleyMode.Table, true), Is.True); Assert.That(flow.Passthrough, Is.True);
            Assert.That(flow.PassthroughLost(), Is.True); Assert.That(flow.Running, Is.EqualTo(ValleyMode.VR)); Assert.That(flow.Chosen, Is.EqualTo(ValleyMode.Table));
            Assert.That(flow.PassthroughLost(), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => flow.Choose((ValleyMode)7, true));
        }
    }
}
