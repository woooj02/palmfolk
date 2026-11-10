using System;
using System.Linq;

namespace Palmfolk.Core.Content
{
    // Hand-authored data for Dario's ValleySpec/director. Not a second simulation.
    public static class ChapterOne
    {
        public static ValleySpec Create(params Obstacle[] measuredObstacles)
        {
            if (measuredObstacles == null) throw new ArgumentNullException(nameof(measuredObstacles));
            var islands = new[] {
                new IslandSpec("S", new Vec3(-0.270, 0.020, -0.150), 0.020),
                new IslandSpec("A", new Vec3(-0.110, 0.020, -0.100), 0.020),
                new IslandSpec("C", new Vec3(0.050, 0.020, -0.100), 0.020),
                new IslandSpec("Sanctuary", new Vec3(0.210, 0.020, -0.150), 0.020),
                new IslandSpec("G", new Vec3(-0.250, 0.020, 0.030), 0.025),
                new IslandSpec("L1", new Vec3(0.210, 0.060, -0.100), 0.020),
                new IslandSpec("L2", new Vec3(0.140, 0.060, 0.050), 0.020),
                new IslandSpec("E", new Vec3(-0.100, 0.020, 0.010), 0.020),
                new IslandSpec("G1", new Vec3(-0.105, 0.020, 0.150), 0.020),
                new IslandSpec("G2", new Vec3(-0.070, 0.020, 0.070), 0.020)
            };
            IslandSpec Island(string id) => islands.First(i => i.Id == id);
            EdgeSpec Edge(string id, string from, string to, double endRadius = 0.02) => EdgeSpec.Between(id, Island(from), Island(to), 0.02, endRadius);
            AnchorZone SharedSource(string from, string a, string b)
            {
                Vec3 facing = (Island(a).Top - Island(from).Top).Normalized + (Island(b).Top - Island(from).Top).Normalized;
                facing = new Vec3(facing.X, 0, facing.Z).Normalized;
                return new AnchorZone(Island(from).Top + facing * Island(from).Radius, 0.02);
            }
            var forkSource = SharedSource("C", "L1", "L2"); var gardenSource = SharedSource("G", "G1", "G2");
            EdgeSpec Fork(string id, string from, string to, AnchorZone source)
            { var rim = Edge(id, from, to, 0.03); return new EdgeSpec(id, from, to, source, rim.ToAnchor); }
            var edges = new[] {
                Edge("S-A", "A", "S"), Edge("A-C", "A", "C"), Edge("C-Sanctuary", "C", "Sanctuary"),
                Fork("C-L1", "C", "L1", forkSource), Fork("C-L2", "C", "L2", forkSource), Edge("C-E", "C", "E"),
                Edge("A-G", "A", "G"), Fork("G-G1", "G", "G1", gardenSource), Fork("G-G2", "G", "G2", gardenSource), Edge("L1-L2", "L1", "L2")
            };
            var puzzles = new[] {
                new PuzzleSpec(PuzzleKind.Board, new[] { "S-A" }, TravelerKind.Walker, "S"),
                new PuzzleSpec(PuzzleKind.FirstBridge, new[] { "S-A" }, TravelerKind.Walker, "S"),
                new PuzzleSpec(PuzzleKind.Width, new[] { "A-C" }, TravelerKind.Group, "A", TravelerKind.Cart),
                new PuzzleSpec(PuzzleKind.Fork, new[] { "C-L1", "C-L2" }, TravelerKind.Walker, "C"),
                new PuzzleSpec(PuzzleKind.Arch, new[] { "C-E" }, TravelerKind.Walker, "C"),
                new PuzzleSpec(PuzzleKind.Finale, new[] { "S-A", "A-C", "C-Sanctuary" }, TravelerKind.Cart, "S")
            };
            return new ValleySpec(islands, edges, puzzles, measuredObstacles,
                new LedgeSpec("S-A", "A", 0.015, 0.06, 180), "S", "Sanctuary");
        }

        // Explicitly provisional H1 test obstacle, matching D3's headless scenario. Not a measured rock.
        public static ValleySpec Greybox()
        {
            var empty = Create(); var edge = empty.Edge("C-E");
            var centre = Vec3.Lerp(edge.FromAnchor.Centre, edge.ToAnchor.Centre, 0.5);
            return Create(new Obstacle(new Vec3(centre.X - 0.008, 0, centre.Z - 0.008), new Vec3(centre.X + 0.008, 0.035, centre.Z + 0.008)));
        }
    }
}
