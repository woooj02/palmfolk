#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Fixtures;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class PresentationTests
    {
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
                    game.Step(frame); var state = new WorldPresentation(game, 0.5); view.Present(state);
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
