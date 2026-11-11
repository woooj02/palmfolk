#if UNITY_5_3_OR_NEWER
using System;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Platform;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Palmfolk.Tests
{
    // D11 (T7) in the Unity editor, counted by Unity's GC.Alloc recorder: a rendered frame of the runner (sample, tick, place, need signs, look and
    // pinch) while the player rests allocates nothing, so the headset's garbage collector has nothing to do between
    // moves. Hands and gaze come from fixed samples here; on the headset the readers refill their buffers.
    public class RunnerFrameTests
    {
        private sealed class StillHands : IHandReader
        {
            public HandSample Left;
            public HandSample Read(HandSide side) => side == HandSide.Left ? Left : null;
            public bool ControllersInHand => false;
        }
        private sealed class StillGaze : IGazeReader
        {
            public GazeSample Sample;
            public GazeSample Read() => Sample;
        }

        private GameObject go, origin; private PalmfolkRunner runner;

        [SetUp] public void Create()
        {
            go = new GameObject("runner"); origin = new GameObject("origin");
            runner = go.AddComponent<PalmfolkRunner>(); runner.ValleyRoot = origin.transform;
            runner.Passthrough = new TableModeTests.FakePassthrough();
        }
        [TearDown] public void Destroy() { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(origin); }

        [Test] public void ARestingFrameAllocatesNothing()
        {
            Assert.That(() => GC.KeepAlive(new byte[64]), Is.AllocatingGCMemory(), "the recorder sees allocations here");
            // Resume past the board (which has no sign): the first bridge's traveler waits for a rope.
            var played = new ValleySimulation(TestValley.Chapter1()); var player = new TestPlayer();
            for (int i = 0; i < 20000 && played.PuzzleIndex < 1; i++)
            {
                long tick = played.Tick + 1;
                played.Step(new HandFrame(tick, tick * 16667, player.Hand(played, tick), null, true, InputCommand.None));
                foreach (var e in played.Events) player.Events.Add(e);
            }
            runner.Open(TestValley.Chapter1(), new SaveData { Valley = played.Progress }, ValleyPose.Identity);
            runner.ChooseMode(ValleyMode.VR);
            var hands = new StillHands(); runner.Hands = hands;
            // A hand resting on the table, palm down, away from the valley: tracked, never boarding.
            hands.Left = new Planted.Placement { Offset = new Vec3(0.6, 0, 0.6) }.Hand(0);
            var head = new Vec3(0, 0.35, -0.3); var gaze = new StillGaze(); runner.Gaze = gaze;
            runner.GazeButtons = null;

            // Walk on until the first traveler waits at its need, then look at its sign.
            double time = 0; var target = NeedSigns.Targets(runner.Game.Valley, runner.Game.Pose);
            for (int i = 0; i < 7200 && target.Count == 0; i++) { runner.Frame(1 / 72.0, time += 1 / 72.0); target = NeedSigns.Targets(runner.Game.Valley, runner.Game.Pose); }
            Assert.That(target.Count, Is.EqualTo(1), $"the first traveler waits ({runner.Game.Valley.Stage}, tick {runner.Game.Tick}, need {NeedSigns.Need(runner.Game.Valley)})");
            gaze.Sample = new GazeSample(head, target[0].Position - head, GazeSource.Head);
            for (int i = 0; i < 120; i++) runner.Frame(1 / 72.0, time += 1 / 72.0);
            Assert.That(runner.Looks.Shown, Is.Not.Null, "the sign is up: the look path runs every frame");
            // Unity's own GC.Alloc recorder: the editor's Mono doesn't count allocations per thread.
            Assert.That(() => { for (int i = 0; i < 720; i++) runner.Frame(1 / 72.0, time += 1 / 72.0); }, Is.Not.AllocatingGCMemory(), "720 resting frames");
            Assert.That(runner.Game.Valley.Stage, Is.EqualTo(DirectorStage.Interacting));
        }
    }
}
#endif
