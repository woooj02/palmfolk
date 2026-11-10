#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Platform;
using UnityEngine;

namespace Palmfolk.Tests
{
    // D9 in the Unity editor: the runner with scripted passthrough and spatial anchors (MR3, MR5). The Meta SDK side
    // needs a headset.
    public class TableModeTests
    {
        internal sealed class FakePassthrough : IPassthrough
        {
            public bool Available { get; set; } = true;
            public bool Visible { get; private set; }
            public void SetVisible(bool visible) => Visible = visible;
        }

        internal sealed class FakeAnchors : ISpatialAnchors
        {
            public readonly Dictionary<string, (Vec3 Position, Vec3 Forward)> Stored = new Dictionary<string, (Vec3, Vec3)>();
            public readonly List<string> Erased = new List<string>();
            public bool Answer = true, FailSaves;
            public Vec3 Drift;
            public Action<bool, Vec3, Vec3> Unanswered;
            private int next;

            public void Save(Vec3 position, Vec3 forward, Action<string> done)
            {
                if (FailSaves) { done(null); return; }
                var id = "anchor-" + ++next; Stored[id] = (position, forward); done(id);
            }
            public void Find(string id, Action<bool, Vec3, Vec3> done)
            {
                if (!Answer) { Unanswered = done; return; }
                if (Stored.TryGetValue(id, out var a)) done(true, a.Position + Drift, a.Forward); else done(false, Vec3.Zero, Vec3.Zero);
            }
            public void Erase(string id) { Erased.Add(id); Stored.Remove(id); }
        }

        private GameObject go, origin; private string folder;
        private PalmfolkRunner runner; private FakeAnchors anchors; private FakePassthrough passthrough;

        [SetUp] public void Create()
        {
            go = new GameObject("runner"); origin = new GameObject("origin");
            runner = go.AddComponent<PalmfolkRunner>(); runner.ValleyRoot = origin.transform; runner.Hands = new PalmfolkRunnerTests.ScriptedHands(runner);
            runner.Anchors = anchors = new FakeAnchors(); runner.Passthrough = passthrough = new FakePassthrough();
            folder = Path.Combine(Path.GetTempPath(), "palmfolk-table-" + Guid.NewGuid().ToString("N"));
        }
        [TearDown] public void Destroy()
        {
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(origin);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        private double clock;
        private int Play(Func<bool> done, int frames = 400000, double hz = 72)
        {
            int n = 0; for (; n < frames && !done(); n++) { clock += 1 / hz; runner.Frame(1 / hz, clock); }
            return n;
        }
        private void Open() => runner.Open(TestValley.Chapter1(), new DirectorySaveStorage(folder));
        private SaveData Saved() { Assert.That(new SaveRepository(new DirectorySaveStorage(folder)).Load(out SaveData s), Is.EqualTo(SaveLoadStatus.Loaded)); return s; }
        private static Vector3 Unity(Vec3 v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);

        // Plants on a fresh launch and plays to the third puzzle.
        private string PlantAndPlay()
        {
            Open();
            Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Planting)); Assert.That(passthrough.Visible, Is.True);
            Play(() => runner.Game.Valley.PuzzleIndex == 2);
            Assert.That(anchors.Stored.Count, Is.EqualTo(1));
            return anchors.Stored.Keys.Single();
        }

        [Test] public void PlantingSavesAnAnchorAndARelaunchOpensThere()
        {
            var id = PlantAndPlay();
            var pose = runner.Game.Pose;
            Assert.That(anchors.Stored[id].Position, Is.EqualTo(pose.Origin)); Assert.That(anchors.Stored[id].Forward, Is.EqualTo(pose.Forward));
            Assert.That(Saved().AnchorId, Is.EqualTo(id)); Assert.That(Saved().Mode, Is.EqualTo(ValleyMode.Table));
            var bridges = runner.Game.Valley.Bridges.Keys.ToList();
            // The headset relocalizes 1 cm away: the valley opens on the anchor, at the planted scale, without planting.
            anchors.Drift = new Vec3(0.01, 0, 0);
            Open();
            Assert.That(runner.Flow.Search, Is.EqualTo(AnchorSearch.Found));
            Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Playing)); Assert.That(runner.Game.Plantings, Is.Zero);
            Assert.That(Vec3.Distance(runner.Game.Pose.Origin, pose.Origin), Is.EqualTo(0.01).Within(1e-9));
            Assert.That(runner.Game.Pose.Scale, Is.EqualTo(pose.Scale).Within(1e-12)); Assert.That(runner.Game.Pose.YawRadians, Is.EqualTo(pose.YawRadians).Within(1e-9));
            Assert.That(Vector3.Distance(origin.transform.position, Unity(runner.Game.Pose.Origin)), Is.LessThan(1e-5f));
            Assert.That(runner.Game.Valley.Bridges.Keys, Is.EquivalentTo(bridges)); Assert.That(runner.Game.Valley.PuzzleIndex, Is.EqualTo(2));
            Assert.That(passthrough.Visible, Is.True); Assert.That(anchors.Stored.Count, Is.EqualTo(1), "no new anchor");
            Play(() => runner.Game.Valley.IsComplete);
            Assert.That(runner.Game.Valley.IsComplete, Is.True);
        }

        [Test] public void AnAnchorNotFoundWithinFiveSecondsMeansReplantingAndNoBridgeIsLost()
        {
            var id = PlantAndPlay();
            var bridges = runner.Game.Valley.Bridges.Keys.ToList();
            anchors.Answer = false;
            Open();
            Assert.That(runner.Game, Is.Null); Assert.That(runner.WaitingForAnchor, Is.True); Assert.That(passthrough.Visible, Is.True, "the room shows while looking");
            double waited = 0;
            while (runner.Game == null && waited < 10) { runner.Frame(1 / 72.0, clock += 1 / 72.0); waited += 1 / 72.0; }
            Assert.That(waited, Is.InRange(5.0, 5.0 + 2 / 72.0));
            Assert.That(runner.Flow.Search, Is.EqualTo(AnchorSearch.Lost));
            Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Planting));
            Assert.That(runner.Game.Valley.Bridges.Keys, Is.EquivalentTo(bridges));
            // The anchor turning up late changes nothing.
            anchors.Unanswered(true, new Vec3(1, 1, 1), new Vec3(0, 0, 1));
            Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Planting)); Assert.That(runner.Game.Pose, Is.Null);
            Play(() => runner.Game.Stage == ValleyStage.Playing);
            Assert.That(anchors.Erased, Is.EqualTo(new[] { id }));
            var replanted = anchors.Stored.Keys.Single(); Assert.That(replanted, Is.Not.EqualTo(id));
            Assert.That(Saved().AnchorId, Is.EqualTo(replanted));
            Assert.That(runner.Game.Valley.Bridges.Keys, Is.EquivalentTo(bridges));
        }

        [Test] public void AnAnchorThatCannotBeSavedAsksToReplantNextLaunch()
        {
            anchors.FailSaves = true;
            Open(); Play(() => runner.Game.Stage == ValleyStage.Playing);
            Assert.That(Saved().AnchorId, Is.Null);
            Open();
            Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Planting)); Assert.That(runner.Flow.Search, Is.EqualTo(AnchorSearch.None));
        }

        [Test] public void WithoutPassthroughTableModeRunsAsVrAtTheSavedPose()
        {
            var id = PlantAndPlay(); var pose = runner.Game.Pose;
            passthrough.Available = false;
            Open();
            Assert.That(runner.Flow.FellBack, Is.True); Assert.That(passthrough.Visible, Is.False);
            Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Playing)); Assert.That(runner.Game.Pose.Origin, Is.EqualTo(pose.Origin));
            var save = runner.Capture();
            Assert.That(save.Mode, Is.EqualTo(ValleyMode.Table), "the player's choice is kept"); Assert.That(save.AnchorId, Is.EqualTo(id));
            Play(() => runner.Game.Valley.IsComplete);
            Assert.That(runner.Game.Valley.IsComplete, Is.True);
        }

        [Test] public void LosingPassthroughMidGameCarriesOnInVr()
        {
            PlantAndPlay(); var pose = runner.Game.Pose; int changed = 0; runner.ModeChanged += _ => changed++;
            passthrough.Available = false;
            Play(() => false, 30);
            Assert.That(runner.Flow.Running, Is.EqualTo(ValleyMode.VR)); Assert.That(passthrough.Visible, Is.False); Assert.That(changed, Is.EqualTo(1));
            Assert.That(runner.Game.Pose, Is.SameAs(pose)); Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Playing));
        }

        // MR5: the waystone switches modes, and a save made in one mode opens in the other.
        [Test] public void ASaveMadeInOneModeOpensInTheOther()
        {
            var id = PlantAndPlay(); var pose = runner.Game.Pose;
            runner.ChooseMode(ValleyMode.VR);
            Assert.That(passthrough.Visible, Is.False); Assert.That(Saved().Mode, Is.EqualTo(ValleyMode.VR));
            Open();
            Assert.That(runner.Flow.Running, Is.EqualTo(ValleyMode.VR)); Assert.That(runner.Game.Pose.Origin, Is.EqualTo(pose.Origin));
            Play(() => runner.Game.Valley.PuzzleIndex == 4);
            // Back to Table mode: the valley is anchored where it stands.
            int changed = 0; runner.ModeChanged += _ => changed++;
            runner.ChooseMode(ValleyMode.Table);
            Assert.That(changed, Is.EqualTo(1)); Assert.That(passthrough.Visible, Is.True);
            Assert.That(anchors.Erased, Does.Contain(id));
            var moved = anchors.Stored.Keys.Single(); Assert.That(Saved().AnchorId, Is.EqualTo(moved)); Assert.That(Saved().Mode, Is.EqualTo(ValleyMode.Table));
            var bridges = runner.Game.Valley.Bridges.Keys.ToList();
            Open();
            Assert.That(runner.Flow.Search, Is.EqualTo(AnchorSearch.Found)); Assert.That(runner.Game.Valley.Bridges.Keys, Is.EquivalentTo(bridges));
            Play(() => runner.Game.Valley.IsComplete);
            Assert.That(runner.Game.Valley.IsComplete, Is.True);
        }

        [Test] public void SetupAddsTableModeToTheRunnerOnce()
        {
            // The project config half (passthrough and anchors in the manifest) is the owner's step; this checks the scene half.
            var mode = Palmfolk.Platform.Editor.LiveInputSetup.AddTableMode();
            try
            {
                Assert.That(mode, Is.Not.Null); Assert.That(mode.gameObject, Is.SameAs(go));
                Assert.That(Palmfolk.Platform.Editor.LiveInputSetup.AddTableMode(), Is.SameAs(mode), "never twice");
            }
            finally { if (mode != null) UnityEngine.Object.DestroyImmediate(mode); }
        }
    }
}
#endif
