#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using Palmfolk.Audio;
using Palmfolk.Core;
using Palmfolk.Core.Content;
using Palmfolk.Platform;
using Palmfolk.UI;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class LivePresentationTests
    {
        [Test] public void AllVisitorWindowsResidentsAndMarksComeFromTheReloadedSave()
        {
            var spec = ChapterOne.Greybox(); var game = new ValleySimulation(spec); var player = new TestPlayer();
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
            for (int i = 0; i < 5; i++)
            {
                if (i > 0) player.Step(game, 1, InputCommand.RingBell);
                int count = i + 1; Assert.That(player.RunUntil(game, () => player.Count(GameEventKind.VisitorSettled) == count, 40000), Is.True);
            }
            var root = new GameObject("Live views test"); var viewer = new GameObject("Head");
            try
            {
                viewer.transform.position = new Vector3(0, 1.5f, -0.4f);
                var runner = root.AddComponent<PalmfolkRunner>();
                runner.Open(spec, SaveCodec.DecodeSave(SaveCodec.EncodeSave(new SaveData { Valley = game.Progress, Mode = ValleyMode.VR, Pose = ValleyPose.Identity })), ValleyPose.Identity);
                var presentation = root.AddComponent<GamePresentation>(); presentation.Configure(runner, viewer.transform);
                var living = root.GetComponentInChildren<LivingValleyView>();
                Assert.That(living.VisibleFigures, Is.EqualTo(6)); Assert.That(living.VisibleMarks, Is.GreaterThanOrEqualTo(5)); Assert.That(presentation.Night.LitWindows, Is.EqualTo(5));
                var windows = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Window ")).ToArray();
                Assert.That(windows.Length, Is.EqualTo(5)); Assert.That(windows.All(w => w.GetComponent<Renderer>().sharedMaterial.GetColor("_EmissionColor").maxColorComponent > 0.5f), Is.True);
                var settings = runner.Player.Copy(); settings.Captions = false; settings.HighContrast = settings.XRay = settings.ReducedMotion = true; runner.Apply(settings);
                runner.Frame(1d / 60, 1);
                Assert.That(root.GetComponentInChildren<XRayView>().Visible, Is.True); Assert.That(root.GetComponentInChildren<ValleyView>().HighContrast, Is.True);
                Assert.That(root.GetComponentInChildren<AudioPresenter>().Captions, Is.False); Assert.That(presentation.Night.ReducedMotion, Is.True);
                Assert.That(runner.Game.Valley.Bridges.Count, Is.EqualTo(game.Bridges.Count));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(viewer); }
        }
        [Test] public void NeedSignsUseDistinctGeometryAndHideWhenGazeLeaves()
        {
            var root = new GameObject("Need signs test");
            try
            {
                var view = root.AddComponent<NeedSignView>(); int[] expected = { 1, 5, 1, 2 }; int index = 0;
                foreach (var sign in new[] { NeedSign.Rope, NeedSign.Plank, NeedSign.Arch, NeedSign.Branches })
                {
                    view.Show(new GazeTarget("Pip", new Vec3(1, 0, 0), 0.03, sign), null, true, 0);
                    Assert.That(view.Showing, Is.EqualTo(sign)); Assert.That(root.GetComponentsInChildren<LineRenderer>().Length, Is.EqualTo(expected[index++]));
                }
                view.Show(null, null, true, 1); Assert.That(view.Showing, Is.EqualTo(NeedSign.None)); Assert.That(root.GetComponentsInChildren<Renderer>(), Is.Empty);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void AnchorSearchShowsPipUntilTheRunnerDecides()
        {
            var root = new GameObject("Anchor search test");
            try
            {
                var view = root.AddComponent<AnchorSearchView>(); view.Show(true, true, 120); Assert.That(view.Finding, Is.True);
                Assert.That(root.GetComponentInChildren<TravelerRig>().CurrentPose, Is.EqualTo(BodyPose.Wave));
                view.Show(false, true, 130); Assert.That(view.Finding, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void VisitorAudioHasActualArrivalAndSettlingEvents()
        {
            Assert.That(CueCatalog.TryMap(GameEventKind.VisitorArrived, out var arrival), Is.True); Assert.That(arrival, Is.EqualTo(Cue.VisitorArrival));
            Assert.That(CueCatalog.TryMap(GameEventKind.VisitorSettled, out var settled), Is.True); Assert.That(settled, Is.EqualTo(Cue.Lantern));
        }
    }
}
#endif
