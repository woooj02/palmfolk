#if UNITY_5_3_OR_NEWER
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Platform;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Palmfolk.Tests
{
    // D6 in the Unity editor: the XR Hands joint mapping, confidence, and the runner placing and saving the valley.
    public class HandJointMapTests
    {
        private static RawJoint[] Joints(XRHandJointTrackingState state, Func<int, Vector3> at = null, float radius = 0.008f) =>
            Enumerable.Range(0, HandJointMap.JointCount).Select(i => new RawJoint(new Pose(at?.Invoke(i) ?? new Vector3(i * 0.01f, 0, 0), Quaternion.identity), radius, state)).ToArray();

        [Test] public void EveryCoreJointHasItsOwnXrJointOfTheSameName()
        {
            Assert.That(HandJointMap.ByCoreJoint.Length, Is.EqualTo(26));
            Assert.That(HandJointMap.ByCoreJoint.Distinct().Count(), Is.EqualTo(26));
            for (int i = 0; i < 26; i++) Assert.That(HandJointMap.ByCoreJoint[i].ToString(), Is.EqualTo(((JointId)i).ToString()));
            Assert.That(HandJointMap.ByCoreJoint, Has.No.Member(XRHandJointID.Invalid));
        }

        [Test] public void ConfidenceFollowsTheRuntimesFidelityOnceItReportsIt()
        {
            var policy = new ConfidencePolicy();
            var pose = XRHandJointTrackingState.Pose | XRHandJointTrackingState.Radius;
            Assert.That(policy.Flags(HandSide.Left, XRHandJointTrackingState.None), Is.EqualTo(JointFlags.None));
            Assert.That(policy.Flags(HandSide.Left, XRHandJointTrackingState.Radius), Is.EqualTo(JointFlags.None), "a radius without a pose is no joint");
            Assert.That(policy.Flags(HandSide.Left, pose), Is.EqualTo(JointFlags.PositionValid | JointFlags.RotationValid | JointFlags.HighConfidence | JointFlags.RadiusValid));
            Assert.That(policy.ReportsFidelity(HandSide.Left), Is.False);
            Assert.That(policy.Flags(HandSide.Left, pose | XRHandJointTrackingState.HighFidelityPose) & JointFlags.HighConfidence, Is.EqualTo(JointFlags.HighConfidence));
            Assert.That(policy.ReportsFidelity(HandSide.Left), Is.True);
            Assert.That(policy.Flags(HandSide.Left, pose) & JointFlags.HighConfidence, Is.EqualTo(JointFlags.None), "inferred joints aren't confident once fidelity is reported");
            Assert.That(policy.Flags(HandSide.Right, pose) & JointFlags.HighConfidence, Is.EqualTo(JointFlags.HighConfidence), "each hand on its own");
            policy.Reset(); Assert.That(policy.ReportsFidelity(HandSide.Left), Is.False);
        }

        [Test] public void JointsAreMovedFromTrackingSpaceToWorldSpace()
        {
            var tracking = Matrix4x4.TRS(new Vector3(1, 2, 3), Quaternion.Euler(0, 90, 0), Vector3.one * 2);
            var hand = HandJointMap.Convert(HandSide.Right, Joints(XRHandJointTrackingState.Pose | XRHandJointTrackingState.Radius), tracking, new ConfidencePolicy());
            Assert.That(hand.Side, Is.EqualTo(HandSide.Right));
            var tip = hand[JointId.LittleTip]; var expected = tracking.MultiplyPoint3x4(new Vector3(0.25f, 0, 0));
            Assert.That(tip.Position.X, Is.EqualTo(expected.x).Within(1e-5)); Assert.That(tip.Position.Z, Is.EqualTo(expected.z).Within(1e-5));
            Assert.That(tip.Radius, Is.EqualTo(0.016).Within(1e-6));
            Assert.That(hand.InvalidMask, Is.EqualTo(0));
            var rotation = new Quaternion((float)tip.Rotation.X, (float)tip.Rotation.Y, (float)tip.Rotation.Z, (float)tip.Rotation.W);
            Assert.That(Quaternion.Angle(rotation, Quaternion.Euler(0, 90, 0)), Is.LessThan(0.01f));
        }

        [Test] public void BrokenJointsArriveInvalidNotAsGarbage()
        {
            var joints = Joints(XRHandJointTrackingState.Pose | XRHandJointTrackingState.Radius, i => i == 10 ? new Vector3(float.NaN, 0, 0) : new Vector3(i, 0, 0));
            joints[3].Radius = float.PositiveInfinity; joints[4].Radius = -1;
            var hand = HandJointMap.Convert(HandSide.Left, joints, Matrix4x4.identity, new ConfidencePolicy());
            Assert.That(hand[JointId.IndexTip].Usable, Is.False);
            Assert.That(hand.InvalidMask, Is.EqualTo(1u << 10));
            Assert.That(hand[JointId.ThumbProximal].HasRadius || hand[JointId.ThumbDistal].HasRadius, Is.False);
            Assert.That(hand[JointId.ThumbProximal].Usable, Is.True);
        }

        [Test] public void WithoutAHeadsetThereAreNoHands()
        {
            var reader = new XRHandsReader();
            Assert.That(reader.HasSubsystem, Is.False);
            Assert.That(reader.Read(HandSide.Left), Is.Null); Assert.That(reader.ControllersInHand, Is.False);
        }
    }

    public class PalmfolkRunnerTests
    {
        // Hands from the headless test player, in world space, as XR Hands would deliver them.
        private sealed class ScriptedHands : IHandReader
        {
            private readonly PalmfolkRunner runner; private readonly TestPlayer player = new TestPlayer();
            private readonly Planted.Placement table = new Planted.Placement { Height = 0.74, YawDegrees = 37, HandLength = 0.2, Offset = new Vec3(0.2, 0, -0.35) };
            public ScriptedHands(PalmfolkRunner runner) { this.runner = runner; runner.Ticked += events =>
            {
                foreach (var e in events)
                    if (e.Kind == GameEventKind.Refused && runner.Game.Valley?.Session != null)
                    { player.Attempts.TryGetValue(runner.Game.Valley.Session.Edge, out int a); player.Attempts[runner.Game.Valley.Session.Edge] = a + 1; }
            }; }
            public HandSample Read(HandSide side)
            {
                var game = runner.Game; long next = game.Tick + 1;
                if (game.Stage == ValleyStage.Planting) return side == HandSide.Left ? table.Hand(next) : null;
                var local = player.Hand(game.Valley, next);
                return local != null && local.Side == side ? game.Pose.ToWorld(local) : null;
            }
            public bool ControllersInHand => false;
        }

        private GameObject go, origin; private string folder;
        private PalmfolkRunner runner;

        [SetUp] public void Create()
        {
            go = new GameObject("runner"); origin = new GameObject("origin");
            runner = go.AddComponent<PalmfolkRunner>(); runner.ValleyRoot = origin.transform; runner.Hands = new ScriptedHands(runner);
            folder = Path.Combine(Path.GetTempPath(), "palmfolk-runner-" + Guid.NewGuid().ToString("N"));
        }
        [TearDown] public void Destroy()
        {
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(origin);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        private int Play(Func<bool> done, int frames = 400000, double hz = 72)
        {
            int n = 0; for (; n < frames && !done(); n++) runner.Frame(1 / hz, n / hz);
            return n;
        }

        [Test] public void PlantingPlacesTheValleyOriginAndTheWholeValleyPlays()
        {
            runner.Open(TestValley.Chapter1(), new DirectorySaveStorage(folder));
            int planted = 0; runner.Ticked += events => planted += events.Count(e => e.Kind == GameEventKind.Planted);
            Play(() => runner.Game.Stage == ValleyStage.Playing);
            Assert.That(planted, Is.EqualTo(1));
            var pose = runner.Game.Pose;
            Assert.That(Vector3.Distance(origin.transform.position, new Vector3((float)pose.Origin.X, (float)pose.Origin.Y, (float)pose.Origin.Z)), Is.LessThan(1e-5f));
            Assert.That(origin.transform.lossyScale.x, Is.EqualTo((float)pose.Scale).Within(1e-5f));
            // The root's transform and Core's pose agree on where a valley point is in the world.
            var local = new Vec3(0.1, 0.02, -0.05); var world = pose.ToWorld(local);
            Assert.That(Vector3.Distance(origin.transform.TransformPoint(0.1f, 0.02f, -0.05f), new Vector3((float)world.X, (float)world.Y, (float)world.Z)), Is.LessThan(1e-5f));
            Play(() => runner.Game.Valley.IsComplete);
            Assert.That(runner.Game.Valley.IsComplete, Is.True);
            Assert.That(runner.Saves, Is.GreaterThan(6), "planting, every bake and every solved puzzle save");
        }

        [Test] public void TheValleyComesBackAfterARestart()
        {
            runner.Open(TestValley.Chapter1(), new DirectorySaveStorage(folder));
            Play(() => runner.Game.Valley != null && runner.Game.Valley.PuzzleIndex == 3);
            var bridges = runner.Game.Valley.Bridges.Keys.ToList();
            // Table mode: reopening plants again and the bridges come back on the new valley.
            runner.Open(TestValley.Chapter1(), new DirectorySaveStorage(folder));
            Assert.That(runner.LoadStatus, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Planting));
            Assert.That(runner.Game.Valley.Bridges.Keys, Is.EquivalentTo(bridges)); Assert.That(runner.Game.Valley.PuzzleIndex, Is.EqualTo(3));
            // VR mode: reopening resumes at the saved pose without planting.
            var save = runner.Capture(); save.Mode = ValleyMode.VR; new SaveRepository(new DirectorySaveStorage(folder)).Save(save);
            runner.Open(TestValley.Chapter1(), new DirectorySaveStorage(folder));
            Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Playing));
            Assert.That(runner.Game.Pose.Origin, Is.EqualTo(save.Pose.Origin));
            Play(() => runner.Game.Valley.IsComplete);
            Assert.That(runner.Game.Valley.IsComplete, Is.True);
        }

        [Test] public void ALongFrameNeverFastForwardsTheGame()
        {
            runner.Open(TestValley.Chapter1(), null);
            // 0.1 s at most per frame: six ticks, give or take the rounding of the leftover.
            Assert.That(runner.Frame(5, 0), Is.InRange(5, 6));
            Assert.That(runner.Frame(0.25, 5), Is.InRange(5, 7));
            Assert.That(runner.Game.Tick + 1, Is.InRange(11, 12));
            Assert.That(runner.Frame(1 / 72.0, 5.1), Is.LessThanOrEqualTo(1));
        }

        [Test] public void CommandsAndSettingsReachTheGame()
        {
            runner.Open(TestValley.Chapter1(), new DirectorySaveStorage(folder));
            Play(() => runner.Game.Stage == ValleyStage.Playing);
            Assert.That(runner.Request(InputCommand.Pause), Is.True); runner.Frame(1 / 30.0, 100);
            Assert.That(runner.Game.IsPaused, Is.True);
            runner.Request(InputCommand.Resume); runner.Frame(1 / 30.0, 100.1);
            Assert.That(runner.Game.IsPaused, Is.False);
            float before = origin.transform.lossyScale.x;
            runner.Apply(new PlayerSettings { WorldScale = 1.5, StillnessToleranceStep = 0 });
            Assert.That(origin.transform.lossyScale.x, Is.EqualTo(before * 1.5f).Within(1e-4f));
            Assert.That(new SaveRepository(new DirectorySaveStorage(folder)).Load(out SaveData saved), Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(saved.Settings.WorldScale, Is.EqualTo(1.5)); Assert.That(saved.Settings.StillnessToleranceStep, Is.EqualTo(0));
        }

        [Test] public void SetupAddsOneWiredRunner()
        {
            UnityEngine.Object.DestroyImmediate(go); go = null;
            var tracking = new GameObject("TrackingSpace");
            try
            {
                var added = Palmfolk.Platform.Editor.LiveInputSetup.AddRunner();
                go = added.gameObject;
                Assert.That(added.TrackingSpace, Is.SameAs(tracking.transform));
                Assert.That(added.ValleyRoot, Is.Not.Null); Assert.That(added.ValleyRoot.name, Is.EqualTo("Valley Origin"));
                Assert.That(Palmfolk.Platform.Editor.LiveInputSetup.AddRunner(), Is.SameAs(added));
                UnityEngine.Object.DestroyImmediate(added.ValleyRoot.gameObject);
            }
            finally { UnityEngine.Object.DestroyImmediate(tracking); }
        }
    }
}
#endif
