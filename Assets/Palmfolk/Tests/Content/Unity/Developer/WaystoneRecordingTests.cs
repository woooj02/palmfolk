#if PALMFOLK_DEVELOPMENT && UNITY_EDITOR
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Core.Content;
using Palmfolk.Developer;
using Palmfolk.Platform;
using Palmfolk.Platform.Developer;
using Palmfolk.UI;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class WaystoneRecordingTests
    {
        private sealed class RecordingTestPassthrough : IPassthrough
        { public bool Available => true; public bool Visible { get; private set; } public void SetVisible(bool value) => Visible = value; }
        [Test] public void ActualWaystoneSettingsAndModesReplayThroughSessionRecording()
        {
            var root = new GameObject("Waystone recording test"); var spec = ChapterOne.Greybox();
            PalmfolkRunner runner = null; SessionRecording session = null;
            try
            {
                runner = root.AddComponent<PalmfolkRunner>(); runner.Passthrough = new RecordingTestPassthrough(); runner.Hands = null;
                session = root.AddComponent<SessionRecording>(); session.TesterConsented = true; session.Runner = runner;
                runner.Open(spec, new SaveData { Mode = ValleyMode.VR }, ValleyPose.Identity);
                var menu = root.AddComponent<WaystoneView>(); var binding = root.AddComponent<WaystoneBinding>(); binding.Configure(runner, menu);
                runner.Frame(1d / 60, 1);
                void Request(WaystoneAction action, double value = 0) => Assert.That(binding.Handle(new WaystoneRequest(action, value, null, false)), Is.True);
                Request(WaystoneAction.Scale, 1.5); Request(WaystoneAction.Stillness, 2); Request(WaystoneAction.ReducedMotion, 1); Request(WaystoneAction.Captions, 0);
                runner.Frame(1d / 60, 2); Request(WaystoneAction.Mode, 1); runner.Frame(1d / 60, 3);
                Request(WaystoneAction.Pause); runner.Frame(1d / 60, 4); Request(WaystoneAction.Resume); runner.Frame(1d / 60, 5);
                Request(WaystoneAction.Mode, 0); runner.Frame(1d / 60, 6);
                var recorded = GameRecordingCodec.Decode(GameRecordingCodec.Encode(session.Recorder.Finish()), Application.version);
                Assert.That(recorded.Changes.Count, Is.GreaterThanOrEqualTo(6));
                var replay = GameReplay.Run(recorded, spec); Assert.That(replay.SameDecisions && replay.SameFinalState, Is.True);
                var save = SaveCodec.DecodeSave(replay.FinalSave); Assert.That(save.Settings.WorldScale, Is.EqualTo(1.5)); Assert.That(save.Settings.Captions, Is.False); Assert.That(save.Mode, Is.EqualTo(ValleyMode.VR));
            }
            finally
            {
                // Synthetic, in-memory test only: reset the recorder before destruction so OnDisable writes no files.
                if (session != null && runner != null) { session.TesterConsented = false; runner.Open(spec, new SaveData(), ValleyPose.Identity); session.Runner = null; }
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
