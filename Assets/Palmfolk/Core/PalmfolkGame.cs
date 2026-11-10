using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public enum ValleyStage { Planting, Playing }

    // The whole game as one tick simulation. Input frames arrive in world space (from tracking);
    // planting turns them into a valley pose, and the puzzle only ever sees valley-local frames.
    // Bridges, routes and travelers are all valley-local, so replanting moves the valley without
    // touching any of them (A4: replanting mid-puzzle keeps every bridge).
    public sealed class PalmfolkGame : ITickSimulation
    {
        private readonly List<GameEvent> events = new List<GameEvent>();
        private bool paused;
        public M1Simulation Puzzle { get; }
        public PlantingDetector Planting { get; }
        public ValleyStage Stage { get; private set; } = ValleyStage.Planting;
        // Null until the first planting; kept while replanting so the old valley stays drawn.
        public ValleyPose Pose { get; private set; }
        public PlantingResult LastPlanting { get; private set; }
        public int Plantings { get; private set; }
        public bool IsPaused => paused;
        public long Tick => Puzzle.Tick;
        public IReadOnlyList<GameEvent> Events => events.AsReadOnly();

        public PalmfolkGame(LevelGeometry level, GameSettings settings = null)
        {
            Puzzle = new M1Simulation(level, settings);
            Planting = new PlantingDetector(settings);
        }

        public void Step(HandFrame frame)
        {
            if (frame == null || frame.Tick != Tick + 1) throw new ArgumentException("Feed exactly one sequential frame per 60 Hz tick.");
            events.Clear();
            if (frame.Command == InputCommand.Pause) paused = true;
            if (frame.Command == InputCommand.Resume) paused = false;
            bool replant = frame.Command == InputCommand.Replant && Stage == ValleyStage.Playing;
            var command = frame.Command == InputCommand.Replant ? InputCommand.None : frame.Command;

            if (replant) { Stage = ValleyStage.Planting; Planting.Reset(); }
            if (Stage == ValleyStage.Planting)
            {
                // The puzzle sits out planting as if focus were lost: a hand that owned Pip goes into
                // recovery, and the player realigns with the ghost after planting.
                Puzzle.Step(new HandFrame(frame.Tick, frame.TimestampMicroseconds, null, null, false, command, frame.ControllersInHand));
                events.AddRange(Puzzle.Events);
                if (replant) Emit(GameEventKind.ReplantStarted);
                var result = Planting.Step(paused ? Unfocused(frame) : frame);
                if (Planting.StartupPromptDue) Emit(GameEventKind.StartupPromptDue);
                if (result != null)
                {
                    Pose = result.Pose; LastPlanting = result; Plantings++; Stage = ValleyStage.Playing;
                    Emit(GameEventKind.Planted);
                }
                return;
            }
            // Replant never reaches this point: it switched the stage above.
            Puzzle.Step(Pose.ToValley(frame));
            events.AddRange(Puzzle.Events);
        }

        private static HandFrame Unfocused(HandFrame frame) =>
            new HandFrame(frame.Tick, frame.TimestampMicroseconds, frame.Left, frame.Right, false, frame.Command, frame.ControllersInHand);
        private void Emit(GameEventKind kind) => events.Add(new GameEvent(Tick, kind));
    }
}
