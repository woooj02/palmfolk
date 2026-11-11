#if PALMFOLK_DEVELOPMENT && UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Palmfolk.Core;
using Palmfolk.Platform;
using UnityEngine;

namespace Palmfolk.Developer
{
    // Counts the exact ticks fed to Core, with no recorder, consent flag, file access or exports.
    public sealed class LiveStatsBinding : MonoBehaviour
    {
        private sealed class MeasuredTicks : ITickSimulation
        {
            public ITickSimulation Inner;
            public PalmfolkGame Game;
            public readonly ValleyDropouts Dropouts = new ValleyDropouts();
            public long Tick => Inner.Tick;
            public IReadOnlyList<GameEvent> Events => Inner.Events;
            public void Step(HandFrame frame) { Inner.Step(frame); Dropouts.Observe(Game, frame); }
        }
        [SerializeField] private PalmfolkRunner runner;
        [SerializeField] private Transform head;
        private MeasuredTicks measured;
        private StatsPanel panel;
        private double seconds, displayedSeconds;
        private int frames;
        private bool placed;
        public double? FramesPerSecond { get; private set; }
        public ValleyDropouts Dropouts => measured?.Dropouts;
        public void Configure(PalmfolkRunner source, Transform viewer = null)
        { Detach(); runner = source; head = viewer; Attach(); }
        private void OnEnable() => Attach();
        private void OnDisable() => Detach();
        private void Attach()
        {
            if (runner == null) return;
            runner.Opened -= Opened; runner.Presented -= Presented;
            runner.Opened += Opened; runner.Presented += Presented;
            if (runner.Game != null) Opened(runner);
        }
        private void Detach()
        {
            if (runner != null)
            {
                runner.Opened -= Opened; runner.Presented -= Presented;
                if (measured != null && runner.StepTarget == measured) runner.StepTarget = measured.Inner;
            }
            measured = null;
        }
        private void Opened(PalmfolkRunner source)
        {
            seconds = displayedSeconds = 0; frames = 0; FramesPerSecond = null;
            measured = new MeasuredTicks { Game = source.Game, Inner = source.StepTarget ?? source.Game };
            source.StepTarget = measured; Refresh();
        }
        public void FrameRendered(double elapsedSeconds)
        {
            if (!Vec3.Finite(elapsedSeconds) || elapsedSeconds <= 0) return;
            seconds += elapsedSeconds; displayedSeconds += elapsedSeconds; frames++;
            if (seconds >= 1) { FramesPerSecond = frames / seconds; seconds = 0; frames = 0; }
        }
        private void Presented(PalmfolkRunner source)
        {
            // Another developer wrapper (for example consented recording) may have attached after Opened.
            // Preserve it and continue measuring the ticks it forwards, without replacing its decisions.
            if (measured == null || measured.Game != source.Game) Opened(source);
            if (source.StepTarget != measured) { measured.Inner = source.StepTarget ?? source.Game; source.StepTarget = measured; }
            FrameRendered(Time.unscaledDeltaTime);
            if (displayedSeconds >= 0.25) { displayedSeconds = 0; Refresh(); }
        }
        public void Refresh()
        {
            if (runner?.Game?.Valley == null || measured == null) return;
            if (panel == null) { var go = new GameObject("Measured developer overlay"); go.transform.SetParent(transform, false); panel = go.AddComponent<StatsPanel>(); }
            var valley = runner.Game.Valley;
            var watcher = valley.IsComplete ? null : measured.Dropouts.Puzzle(valley.PuzzleIndex);
            string puzzle = valley.IsComplete ? "Chapter totals / visitors" : valley.Puzzle.Kind + " (" + (valley.PuzzleIndex + 1) + "/6)";
            string mode = (Application.isEditor ? "Unity editor / " : "") + (runner.Flow?.Running.ToString() ?? "mode unavailable") + (runner.Flow?.FellBack == true ? " (fallback)" : "");
            panel.Show(FramesPerSecond, watcher?.Count ?? measured.Dropouts.Count, watcher?.DisruptiveCount ?? measured.Dropouts.DisruptiveCount,
                puzzle, runner.GazeSource.ToString(), mode, Application.version);
            if (runner.Game.Pose != null)
            { panel.transform.position = ToUnity(runner.Game.Pose.ToWorld(new Vec3(0.17, 0.13, -0.18))); panel.transform.localScale = Vector3.one * (float)runner.Game.Pose.Scale; placed = true; }
            else if (!placed && head != null) { panel.transform.position = head.position + head.forward * 0.45f + head.right * 0.12f; placed = true; }
            if (head != null && (panel.transform.position - head.position).sqrMagnitude > 1e-6f) panel.transform.rotation = Quaternion.LookRotation(panel.transform.position - head.position);
        }
        private static Vector3 ToUnity(Vec3 value) => new Vector3((float)value.X, (float)value.Y, (float)value.Z);
    }
}
#endif
