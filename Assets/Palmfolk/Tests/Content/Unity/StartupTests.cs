#if UNITY_EDITOR
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Fixtures;
using Palmfolk.UI;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class StartupTests
    {
        [Test] public void PlantedGrowthOnlyChangesPresentationScale()
        {
            var root = new GameObject("Growth test");
            try
            {
                var level = new LevelGeometry(new Vec3(-0.06, 0.006, 0), new Vec3(0.06, 0.006, 0));
                var game = new PalmfolkGame(level); var source = new HandSequence();
                for (long i = 0; i < 100 && game.Stage == ValleyStage.Planting; i++) { source.TryRead(i, out var frame); game.Step(frame); }
                Assert.That(game.Pose, Is.Not.Null);
                var view = root.AddComponent<PalmfolkView>(); view.Configure(level);
                var pose = game.Pose;
                view.Consume(new[] { new GameEvent(game.Tick, GameEventKind.Planted) });
                view.Present(new WorldPresentation(game, 1));
                Assert.That(view.ValleyRoot.localScale.x, Is.LessThan((float)pose.Scale));
                view.ReducedMotion = true; view.Present(new WorldPresentation(game, 1));
                Assert.That(view.ValleyRoot.localScale.x, Is.EqualTo((float)pose.Scale).Within(1e-6));
                Assert.That(game.Pose, Is.SameAs(pose));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void StartupDistinguishesHeldSetDownAndMissingHands()
        {
            var root = new GameObject("Startup test");
            try
            {
                var view = root.AddComponent<StartupView>();
                var level = new LevelGeometry(new Vec3(-0.06, 0.006, 0), new Vec3(0.06, 0.006, 0));
                var held = new PalmfolkGame(level);
                for (int i = 0; i < 610; i++) held.Step(new HandFrame(i, i * 1000000L / 60, null, null, controllersInHand: true));
                view.Present(new WorldPresentation(held, 1));
                Assert.That(view.ShowingControllers, Is.True); Assert.That(view.Prompt, Does.Contain("controllers"));
                held.Step(new HandFrame(610, 610 * 1000000L / 60, new HandSequence().Sample(610), null));
                view.Present(new WorldPresentation(held, 1));
                Assert.That(view.ShowingControllers, Is.False); Assert.That(view.Prompt, Is.Empty);
                var missing = new PalmfolkGame(level);
                for (int i = 0; i < 610; i++) missing.Step(new HandFrame(i, i * 1000000L / 60, null, null));
                view.Present(new WorldPresentation(missing, 1));
                Assert.That(view.ShowingControllers, Is.False); Assert.That(view.Prompt, Is.EqualTo("Bring a hand into view."));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
