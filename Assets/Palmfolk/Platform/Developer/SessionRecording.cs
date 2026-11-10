#if PALMFOLK_DEVELOPMENT
using System;
using System.IO;
using System.Text;
using Palmfolk.Core;
using Palmfolk.Developer;
using UnityEngine;

namespace Palmfolk.Platform.Developer
{
    // Developer builds only (T13, T17a, H3). With the tester's verbal consent ticked, records the session from the
    // moment the runner opens a game, and on pause or quit writes the recording and the joint-radius log to the
    // app's private storage (recordings/, never committed). Pull them with adb; replay in the editor with GameReplay.
    public sealed class SessionRecording : MonoBehaviour
    {
        [SerializeField] private PalmfolkRunner runner;
        [SerializeField] private bool testerConsented;
        [SerializeField] private string contentId = "chapter-one-greybox";

        public GameRecorder Recorder { get; private set; }
        public string LastWritten { get; private set; }
        public bool TesterConsented { get => testerConsented; set => testerConsented = value; }
        public PalmfolkRunner Runner { get => runner; set { Detach(); runner = value; Attach(); } }

        private void OnEnable() => Attach();
        private void OnDisable() { Write(); Detach(); }

        private void Attach()
        {
            if (runner == null) return;
            runner.Opened -= Begin; runner.Opened += Begin;
            if (runner.Game != null && Recorder == null && runner.Game.Plantings == 0 && runner.Game.Events.Count == 0) Begin(runner);
        }
        private void Detach() { if (runner != null) runner.Opened -= Begin; }

        private void Begin(PalmfolkRunner opened)
        {
            Recorder = null;
            if (!testerConsented) return;
            Recorder = new GameRecorder(true, Application.version, contentId, opened.Game, opened.OpenedFrom, opened.OpenedPose, opened.BaseSettings);
            opened.StepTarget = Recorder;
        }

        private void OnApplicationPause(bool pausing) { if (pausing) Write(); }
        private void OnApplicationQuit() => Write();

        // Writes what's recorded so far; recording continues after a pause.
        public void Write() => Write(Path.Combine(Application.persistentDataPath, "recordings"));

        public void Write(string folder)
        {
            if (Recorder == null || Recorder.FrameCount == 0) return;
            Directory.CreateDirectory(folder);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string path = Path.Combine(folder, "session-" + stamp + ".pfr2");
            File.WriteAllBytes(path, GameRecordingCodec.Encode(Recorder.Finish()));
            File.WriteAllText(Path.Combine(folder, "session-" + stamp + "-radii.txt"), RadiusLog(Recorder.Radii, runner?.Hands as XRHandsReader));
            LastWritten = path;
        }

        // H3: per joint, how often a radius was reported with a confident pose, and its range (millimetres).
        public static string RadiusLog(RadiusWatcher radii, XRHandsReader reader)
        {
            var text = new StringBuilder();
            text.AppendLine("side,joint,valid,missing,min_mm,max_mm,mean_mm");
            foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
                foreach (JointId joint in Enum.GetValues(typeof(JointId)))
                {
                    var s = radii.Summary(side, joint);
                    text.AppendLine($"{side},{joint},{s.Valid},{s.Missing},{s.Minimum * 1000:F2},{s.Maximum * 1000:F2},{s.Mean * 1000:F2}");
                }
            if (reader != null)
                text.AppendLine($"# runtime reports high-fidelity joints: left {reader.Confidence.ReportsFidelity(HandSide.Left)}, right {reader.Confidence.ReportsFidelity(HandSide.Right)}");
            return text.ToString();
        }
    }
}
#endif
