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
        // The puzzle in valley-local space: the M1 slice (M1Simulation) or the whole valley (ValleySimulation).
        public ITickSimulation Simulation { get; }
        public M1Simulation Puzzle => Simulation as M1Simulation;
        public ValleySimulation Valley => Simulation as ValleySimulation;
        public PlantingDetector Planting { get; }
        public ValleyStage Stage { get; private set; } = ValleyStage.Planting;
        // Null until the first planting; kept while replanting so the old valley stays drawn.
        public ValleyPose Pose { get; private set; }
        public PlantingResult LastPlanting { get; private set; }
        public int Plantings { get; private set; }
        public bool IsPaused => paused;
        public long Tick => Simulation.Tick;
        public IReadOnlyList<GameEvent> Events => events.AsReadOnly();

        public PalmfolkGame(LevelGeometry level, GameSettings settings = null) : this(new M1Simulation(level, settings), settings) { }
        public PalmfolkGame(ValleySpec valley, GameSettings settings = null, ValleyProgress resume = null) : this(new ValleySimulation(valley, settings, resume), settings) { }
        // Resume at a known pose (a found spatial anchor, or VR mode's saved pose): play starts without planting.
        public PalmfolkGame(ValleySpec valley, ValleyPose pose, GameSettings settings = null, ValleyProgress resume = null) : this(valley, settings, resume)
        {
            Pose = pose ?? throw new ArgumentNullException(nameof(pose)); Stage = ValleyStage.Playing;
        }
        // Opens a save (D5): the valley's progress, the player's settings, and the pose if one is given.
        public static PalmfolkGame Open(ValleySpec valley, SaveData save, ValleyPose pose = null, GameSettings settings = null)
        {
            var applied = (save?.Settings ?? new PlayerSettings()).Apply(settings);
            return pose == null ? new PalmfolkGame(valley, applied, save?.Valley) : new PalmfolkGame(valley, pose, applied, save?.Valley);
        }
        // What to save now. Mode and anchor belong to the platform; they're carried over from `previous`.
        public SaveData Capture(PlayerSettings player, SaveData previous = null) => new SaveData
        {
            Valley = Valley?.Progress ?? previous?.Valley, M1 = Puzzle?.Capture() ?? previous?.M1, Pose = Pose ?? previous?.Pose,
            Mode = previous?.Mode ?? ValleyMode.Table, AnchorId = previous?.AnchorId, Settings = (player ?? previous?.Settings ?? new PlayerSettings()).Copy()
        };
        // Applies the player's settings now: stillness tolerance at once, world scale by rescaling the valley about its
        // origin (bridges are valley-local, so nothing moves relative to the valley).
        public void Apply(PlayerSettings player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            player.Validate();
            Valley?.SetStillnessTolerance(player.StillnessToleranceStep); Puzzle?.SetStillnessTolerance(player.StillnessToleranceStep);
            double before = Planting.WorldScale; Planting.SetWorldScale(player.WorldScale);
            if (Pose != null && player.WorldScale != before) Pose = new ValleyPose(Pose.Origin, Pose.Forward, Pose.Scale * player.WorldScale / before);
        }
        private PalmfolkGame(ITickSimulation simulation, GameSettings settings)
        {
            Simulation = simulation;
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
                Simulation.Step(new HandFrame(frame.Tick, frame.TimestampMicroseconds, null, null, false, command, frame.ControllersInHand));
                events.AddRange(Simulation.Events);
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
            Simulation.Step(Pose.ToValley(frame));
            events.AddRange(Simulation.Events);
        }

        private static HandFrame Unfocused(HandFrame frame) =>
            new HandFrame(frame.Tick, frame.TimestampMicroseconds, frame.Left, frame.Right, false, frame.Command, frame.ControllersInHand);
        private void Emit(GameEventKind kind) => events.Add(new GameEvent(Tick, kind));
    }
}
