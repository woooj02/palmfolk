#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Core.Content;
using Palmfolk.Fixtures;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class PresentationTests
    {
        [Test] public void FullValleyPresentationDrawsTheActualSavedBridgesAndGraph()
        {
            var root = new GameObject("Valley view test");
            try
            {
                var game = new ValleySimulation(ChapterOne.Greybox()); var player = new TestPlayer();
                var view = root.AddComponent<ValleyView>(); view.Present(new ValleyPresentation(game, 1));
                Assert.That(view.ValleyRoot.Find("Islands and obstacles").childCount, Is.EqualTo(11));
                Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
                var frame = new ValleyPresentation(game, 0.5); view.Present(frame);
                Assert.That(view.RenderedBridgeCount, Is.EqualTo(game.Bridges.Count));
                Assert.That(view.ValleyRoot.Find("Pip").localPosition.x, Is.EqualTo((float)frame.Pip.X).Within(1e-6));
                var xray = root.AddComponent<XRayView>(); xray.SetVisible(true); xray.Present(frame);
                Assert.That(root.GetComponentsInChildren<LineRenderer>().Length, Is.GreaterThan(10));
                var night = root.AddComponent<NightView>(); night.Configure(game.Valley.Island("Sanctuary").Top); night.Present(frame);
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(game.IsComplete, Is.True); Assert.That(game.Bram.Crossed, Is.EqualTo(3));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void ReducedNightFadeIsMonotonicAndUnverifiedTableDimmingIsZero()
        {
            var root = new GameObject("Night test");
            try
            {
                var night = root.AddComponent<NightView>(); night.Configure(new Vec3(0.21, 0.02, -0.15));
                night.ReducedMotion = true; night.TableMode = true; float requested = -1;
                night.TableDimmingRequested += value => requested = value;
                night.SetState(3, 2, false, null); float previous = night.Intensity;
                for (int i = 0; i < 10; i++) { night.Advance(0.1f); Assert.That(night.Intensity, Is.LessThanOrEqualTo(previous)); previous = night.Intensity; }
                Assert.That(requested, Is.Zero); Assert.That(night.LitWindows, Is.EqualTo(2));
                night.SetState(3, 2, true, null); night.Advance(1);
                Assert.That(night.Intensity, Is.EqualTo(1).Within(1e-6), "Finale restores light.");
                Assert.That(root.GetComponentsInChildren<Camera>(true), Is.Empty);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void OriginalRigHasThreeCentimetreBodyAndDistinctPoses()
        {
            var root = new GameObject("Rig test"); var material = new Material(Shader.Find("Standard"));
            try
            {
                var rig = root.AddComponent<TravelerRig>(); rig.Initialize(material);
                rig.Show(BodyPose.Wave, 0); var wave = root.transform.Find("Right arm").localRotation;
                rig.Show(BodyPose.Grip, 0); Assert.That(root.transform.Find("Head").localPosition.y, Is.LessThan(0.026f));
                rig.Show(BodyPose.Celebrate, 0); Assert.That(root.transform.Find("Right arm").localRotation, Is.Not.EqualTo(wave));
                Assert.That(root.transform.Find("Head").localPosition.y + 0.004f, Is.EqualTo(0.03f).Within(1e-6));
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(material); }
        }
        [Test] public void StatsTypeMatchesTheCompiledDevelopmentGate()
        {
            var stats = System.Type.GetType("Palmfolk.Developer.StatsPanel, Palmfolk.Stats");
            bool requested = System.Environment.GetEnvironmentVariable("PALMFOLK_CHECK_DEVELOPMENT") == "true";
            if (System.Environment.GetEnvironmentVariable("PALMFOLK_CHECK_DEVELOPMENT") != null)
                Assert.That(stats != null, Is.EqualTo(requested), "Gate must match the wrapper's requested configuration.");
#if PALMFOLK_DEVELOPMENT
            Assert.That(stats, Is.Not.Null);
#else
            Assert.That(stats, Is.Null);
#endif
        }
        [Test] public void ShippingXRayToggleDoesNotMutateCore()
        {
            var root = new GameObject("X-ray test");
            try
            {
                var level = new LevelGeometry(new Vec3(-0.06, 0.006, 0), new Vec3(0.06, 0.006, 0));
                var game = new M1Simulation(level); var xray = root.AddComponent<XRayView>(); xray.Configure(level);
                xray.SetVisible(true); xray.Present(new WorldPresentation(game, 1));
                Assert.That(root.GetComponentsInChildren<LineRenderer>().Length, Is.GreaterThan(0));
                xray.SetVisible(false);
                Assert.That(root.GetComponentsInChildren<LineRenderer>(), Is.Empty);
                Assert.That(game.Tick, Is.EqualTo(-1));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void ReplayDrivesViewWithoutChangingTheSimulation()
        {
            var level = new LevelGeometry(new Vec3(-0.06, 0.006, 0), new Vec3(0.06, 0.006, 0));
            var game = new M1Simulation(level, new GameSettings { TravelerSpeed = 0.3 });
            var root = new GameObject("View test");
            try
            {
                var view = root.AddComponent<PalmfolkView>(); view.Configure(level);
                foreach (var frame in new HandSequence(ticks: 900, withdrawStart: 400, withdrawEnd: 430, withdrawal: new Vec3(0, 0, 0.2)).Frames())
                {
                    game.Step(frame); view.Consume(game.Events); var state = new WorldPresentation(game, 0.5); view.Present(state);
                    Assert.That(game.Tick, Is.EqualTo(frame.Tick));
                }
                Assert.That(game.Phase, Is.EqualTo(M1Phase.Complete));
                var persistent = view.ValleyRoot.Find("Persistent bridge");
                var before = persistent.GetComponentsInChildren<Transform>().Select(t => t.localPosition).ToArray();
                view.Present(new WorldPresentation(game, 1));
                Assert.That(persistent.GetComponentsInChildren<Transform>().Select(t => t.localPosition), Is.EqualTo(before));
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(view.ValleyRoot.Find("Pip").localPosition.x, Is.EqualTo((float)game.Pip.X).Within(1e-6));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
