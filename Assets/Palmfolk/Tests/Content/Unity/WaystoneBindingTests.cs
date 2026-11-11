#if UNITY_EDITOR
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Core.Content;
using Palmfolk.Fixtures;
using Palmfolk.Platform;
using Palmfolk.UI;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class WaystoneBindingTests
    {
        private sealed class Hands : IHandReader
        {
            public HandSample Left, Right;
            public bool ControllersInHand { get; set; }
            public HandSample Read(HandSide side) => side == HandSide.Left ? Left : Right;
        }
        private static WaystoneRequest Request(WaystoneAction action, double value = 0) => new WaystoneRequest(action, value, HandSide.Left, false);
        [Test] public void CommandsUseTheClockAndSettingsPersistThroughTheRunner()
        {
            var root = new GameObject("Live waystone test");
            try
            {
                var runner = root.AddComponent<PalmfolkRunner>(); runner.Hands = new Hands();
                runner.Open(ChapterOne.Greybox(), new SaveData { Mode = ValleyMode.VR }, ValleyPose.Identity);
                var view = root.AddComponent<WaystoneView>(); var binding = root.AddComponent<WaystoneBinding>(); binding.Configure(runner, view);
                view.RequestSlot(0); Assert.That(runner.Game.IsPaused, Is.False, "request waits for a tick");
                runner.Frame(1d / 60, 1); Assert.That(runner.Game.IsPaused, Is.True);
                view.RequestSlot(0); runner.Frame(1d / 60, 2); Assert.That(runner.Game.IsPaused, Is.False);
                Assert.That(binding.Handle(Request(WaystoneAction.Stillness, 2)), Is.True);
                binding.Handle(Request(WaystoneAction.Scale, 1.5)); binding.Handle(Request(WaystoneAction.Captions, 0));
                binding.Handle(Request(WaystoneAction.ReducedMotion, 1)); binding.Handle(Request(WaystoneAction.HighContrast, 1)); binding.Handle(Request(WaystoneAction.XRay, 1));
                var saved = SaveCodec.DecodeSave(SaveCodec.EncodeSave(runner.Capture()));
                Assert.That(saved.Settings.WorldScale, Is.EqualTo(1.5)); Assert.That(saved.Settings.StillnessToleranceStep, Is.EqualTo(2));
                Assert.That(saved.Settings.Captions, Is.False); Assert.That(saved.Settings.ReducedMotion && saved.Settings.HighContrast && saved.Settings.XRay, Is.True);
                binding.Handle(Request(WaystoneAction.Restart)); binding.Handle(Request(WaystoneAction.Bell));
                Assert.That(runner.Source.QueuedCommands, Is.EqualTo(2)); runner.Frame(2d / 60, 3);
                binding.Handle(Request(WaystoneAction.Mode, 1));
                Assert.That(runner.Flow.Chosen, Is.EqualTo(ValleyMode.Table)); Assert.That(runner.Flow.Running, Is.EqualTo(ValleyMode.VR), "honest unavailable passthrough fallback");
                bool quit = false; binding.Quit = () => quit = true; binding.Handle(Request(WaystoneAction.Quit)); Assert.That(quit, Is.True);
                binding.Handle(Request(WaystoneAction.Replant)); runner.Frame(1d / 60, 4); Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Planting));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [TestCase(HandSide.Left)] [TestCase(HandSide.Right)]
        public void FivePokeTipsFollowEachTrackedHandAndDisableOnLoss(HandSide side)
        {
            var root = new GameObject("Finger input test");
            try
            {
                var runner = root.AddComponent<PalmfolkRunner>(); var hands = new Hands(); runner.Hands = hands;
                runner.Open(ChapterOne.Greybox(), new SaveData(), ValleyPose.Identity);
                var sample = new HandSequence(side: side).Sample(0);
                if (side == HandSide.Left) hands.Left = sample; else hands.Right = sample;
                var pokes = root.AddComponent<WaystonePokes>(); pokes.Configure(runner); runner.Frame(1d / 60, 1);
                for (int i = 0; i < 10; i++) Assert.That(pokes.Finger(i).enabled, Is.EqualTo((i < 5) == (side == HandSide.Left)));
                pokes.Configure(null); for (int i = 0; i < 10; i++) Assert.That(pokes.Finger(i).enabled, Is.False, "detaching removes stale live input");
                pokes.Configure(runner);
                hands.Left = hands.Right = null; runner.Frame(1d / 60, 2);
                for (int i = 0; i < 10; i++) Assert.That(pokes.Finger(i).enabled, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void WorldGazeButtonsTrackTheActualWaystoneAndDetachCleanly()
        {
            var root = new GameObject("Gaze menu test");
            try
            {
                var runner = root.AddComponent<PalmfolkRunner>(); runner.Open(ChapterOne.Greybox(), new SaveData(), ValleyPose.Identity);
                var menu = root.AddComponent<WaystoneView>(); menu.transform.position = new Vector3(2, 1, 3);
                var binding = root.AddComponent<WaystoneBinding>(); binding.Configure(runner, menu);
                var buttons = runner.GazeButtons(); Assert.That(buttons.Count, Is.EqualTo(4));
                Assert.That(buttons[0].WidthDegrees, Is.EqualTo(6)); Assert.That(buttons[0].Centre.X, Is.GreaterThan(1));
                menu.SetHovered(buttons[0].Id); Assert.That(root.transform.Find("Gaze target outline").gameObject.activeSelf, Is.True);
                binding.Configure(null, null); Assert.That(runner.GazeButtons, Is.Null);
                menu.RequestSlot(0); Assert.That(runner.Source.QueuedCommands, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
