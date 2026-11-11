using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public sealed class FixedTickRunner
    {
        private double accumulator;
        private readonly List<GameEvent> events = new List<GameEvent>();
        private readonly IReadOnlyList<GameEvent> view;
        public FixedTickRunner() { view = events.AsReadOnly(); }
        public IReadOnlyList<GameEvent> Events => view;
        public double InterpolationAlpha => Vec3.Clamp(accumulator / GameSettings.Delta, 0, 1);
        public int Advance(double renderedSeconds, IHandFrameSource input, ITickSimulation simulation, int maxTicks = 8)
        {
            if (!Vec3.Finite(renderedSeconds) || renderedSeconds < 0 || input == null || simulation == null || maxTicks < 1) throw new ArgumentException("Invalid fixed-tick input.");
            accumulator += renderedSeconds; int count = 0; events.Clear();
            while (accumulator + 1e-12 >= GameSettings.Delta && count < maxTicks)
            {
                if (!input.TryRead(simulation.Tick + 1, out var frame)) break;
                simulation.Step(frame); var raised = simulation.Events; for (int i = 0; i < raised.Count; i++) events.Add(raised[i]); accumulator = Math.Max(0, accumulator - GameSettings.Delta); count++;
            }
            return count; // Backlog is retained; recorded ticks are never silently dropped.
        }
        public Vec3 InterpolatedPip(M1Simulation game) => Vec3.Lerp(game.PreviousPip, game.Pip, InterpolationAlpha);
        public Vec3 InterpolatedBram(M1Simulation game) => Vec3.Lerp(game.PreviousBram, game.Bram, InterpolationAlpha);
    }
}
