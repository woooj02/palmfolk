using System;
using System.Collections.Generic;
using System.Linq;

namespace Palmfolk.Core
{
    // Valley content types (D3). Sam's content (Core/Content) fills these; the director runs them.
    // Every position is valley-local (see core-api.md), at default world scale.

    // An anchor is a zone, not a point: any route end inside it connects (Fork: "zones sized to fit any planted hand").
    public readonly struct AnchorZone
    {
        public readonly Vec3 Centre;
        public readonly double Radius;
        public AnchorZone(Vec3 centre, double radius)
        {
            if (!centre.IsFinite || !Vec3.Finite(radius) || radius <= 0 || radius > 0.2) throw new ArgumentException("Invalid anchor zone.");
            Centre = centre; Radius = radius;
        }
        public bool Contains(Vec3 point) => Vec3.Distance(point, Centre) <= Radius;
        // What the connection watcher sees: anywhere inside the zone reads as at most ConnectDistance away,
        // so the hold and break distances (T2) keep their meaning at the zone's edge.
        public double ConnectionDistance(Vec3 point, GameSettings settings) =>
            Math.Max(0, Vec3.Distance(point, Centre) - Math.Max(0, Radius - settings.ConnectDistance));
    }

    public sealed class IslandSpec
    {
        public string Id { get; }
        // Centre of the island's deck top.
        public Vec3 Top { get; }
        public double Radius { get; }
        public IslandSpec(string id, Vec3 top, double radius)
        {
            if (string.IsNullOrWhiteSpace(id) || !top.IsFinite || !Vec3.Finite(radius) || radius <= 0 || radius > 0.3) throw new ArgumentException("Invalid island.");
            Id = id; Top = top; Radius = radius;
        }
    }

    // A gap between two islands. Pip builds from `From` when he can reach it, otherwise from `To`.
    public sealed class EdgeSpec
    {
        public string Id { get; }
        public string From { get; }
        public string To { get; }
        public AnchorZone FromAnchor { get; }
        public AnchorZone ToAnchor { get; }
        public EdgeSpec(string id, string from, string to, AnchorZone fromAnchor, AnchorZone toAnchor)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || from == to) throw new ArgumentException("Invalid edge.");
            if (Vec3.Distance(fromAnchor.Centre, toAnchor.Centre) < 0.01) throw new ArgumentException("Edge anchors overlap.");
            Id = id; From = from; To = to; FromAnchor = fromAnchor; ToAnchor = toAnchor;
        }
        // Anchors on the island rims facing each other (docs/testing/valley-layout.md).
        public static EdgeSpec Between(string id, IslandSpec from, IslandSpec to, double fromZone, double toZone)
        {
            if (from == null || to == null) throw new ArgumentNullException();
            var flat = new Vec3(to.Top.X - from.Top.X, 0, to.Top.Z - from.Top.Z).Normalized;
            if (flat.Length == 0) throw new ArgumentException("Islands are stacked.");
            return new EdgeSpec(id, from.Id, to.Id, new AnchorZone(from.Top + flat * from.Radius, fromZone), new AnchorZone(to.Top - flat * to.Radius, toZone));
        }
        public bool Touches(string island) => From == island || To == island;
        public string Other(string island) => island == From ? To : island == To ? From : throw new ArgumentException("Island not on edge.");
        public AnchorZone AnchorAt(string island) => island == From ? FromAnchor : island == To ? ToAnchor : throw new ArgumentException("Island not on edge.");
    }

    // V2: the narrow ledge on one island end of one edge. Every bridge built there is capped to the ledge width,
    // so the first bridge is always too narrow for the cart; Pip widens the ledge before the finale.
    public sealed class LedgeSpec
    {
        public string Edge { get; }
        public string Island { get; }
        public double NarrowWidth { get; }
        public double WideWidth { get; }
        public int WideningTicks { get; }
        public LedgeSpec(string edge, string island, double narrowWidth, double wideWidth, int wideningTicks)
        {
            if (string.IsNullOrWhiteSpace(edge) || string.IsNullOrWhiteSpace(island) || !Vec3.Finite(narrowWidth) || !Vec3.Finite(wideWidth) ||
                narrowWidth <= 0 || wideWidth <= narrowWidth || wideningTicks < 1 || wideningTicks > 36000) throw new ArgumentException("Invalid ledge.");
            Edge = edge; Island = island; NarrowWidth = narrowWidth; WideWidth = wideWidth; WideningTicks = wideningTicks;
        }
    }

    public enum PuzzleKind { Board, FirstBridge, Width, Fork, Arch, Finale }

    // One of the six puzzles. Board carries Pip on the hand across its single edge, starting from TravelerFrom.
    // The others build every listed edge to the Traveler's rules; then a traveler crosses each edge away from
    // TravelerFrom. Finale lists Bram's route from the camp to the sanctuary, in order, and uses the cart's rules.
    // Rule is what a bridge must pass; it defaults to the traveler's own rules. Width's group can be held to the
    // cart's rules so the finale reuses that bridge as it is (V1).
    public sealed class PuzzleSpec
    {
        public PuzzleKind Kind { get; }
        public IReadOnlyList<string> Edges { get; }
        public TravelerKind Traveler { get; }
        public string TravelerFrom { get; }
        public TravelerKind Rule { get; }
        public PuzzleSpec(PuzzleKind kind, string[] edges, TravelerKind traveler, string travelerFrom, TravelerKind? rule = null)
        {
            if (!Enum.IsDefined(typeof(PuzzleKind), kind) || !Enum.IsDefined(typeof(TravelerKind), traveler) || edges == null || edges.Length == 0 ||
                (rule.HasValue && !Enum.IsDefined(typeof(TravelerKind), rule.Value)) ||
                edges.Any(string.IsNullOrWhiteSpace) || edges.Distinct().Count() != edges.Length || string.IsNullOrWhiteSpace(travelerFrom)) throw new ArgumentException("Invalid puzzle.");
            Kind = kind; Edges = Array.AsReadOnly((string[])edges.Clone()); Traveler = traveler; TravelerFrom = travelerFrom; Rule = rule ?? traveler;
        }
    }

    // The whole valley. Obstacles (the rock) are axis-aligned boxes in valley-local space.
    public sealed class ValleySpec
    {
        private readonly Dictionary<string, IslandSpec> islands = new Dictionary<string, IslandSpec>();
        private readonly Dictionary<string, EdgeSpec> edges = new Dictionary<string, EdgeSpec>();
        public IReadOnlyList<IslandSpec> Islands { get; }
        public IReadOnlyList<EdgeSpec> Edges { get; }
        public IReadOnlyList<PuzzleSpec> Puzzles { get; }
        public ObstacleField Clearance { get; }
        public LedgeSpec Ledge { get; }
        public string Camp { get; }
        public string Sanctuary { get; }

        public ValleySpec(IslandSpec[] islands, EdgeSpec[] edges, PuzzleSpec[] puzzles, Obstacle[] obstacles, LedgeSpec ledge, string camp, string sanctuary)
        {
            if (islands == null || edges == null || puzzles == null || obstacles == null || islands.Any(i => i == null) || edges.Any(e => e == null) || puzzles.Any(p => p == null))
                throw new ArgumentException("Incomplete valley.");
            foreach (var island in islands) if (!this.islands.ContainsKey(island.Id)) this.islands.Add(island.Id, island); else throw new ArgumentException("Duplicate island " + island.Id);
            foreach (var edge in edges)
            {
                if (this.edges.ContainsKey(edge.Id)) throw new ArgumentException("Duplicate edge " + edge.Id);
                if (!this.islands.ContainsKey(edge.From) || !this.islands.ContainsKey(edge.To)) throw new ArgumentException("Edge " + edge.Id + " names an unknown island.");
                this.edges.Add(edge.Id, edge);
            }
            Islands = Array.AsReadOnly((IslandSpec[])islands.Clone()); Edges = Array.AsReadOnly((EdgeSpec[])edges.Clone());
            Puzzles = Array.AsReadOnly((PuzzleSpec[])puzzles.Clone()); Clearance = new ObstacleField(obstacles);
            Ledge = ledge; Camp = camp; Sanctuary = sanctuary;
            Validate();
        }

        public IslandSpec Island(string id) => id != null && islands.TryGetValue(id, out var island) ? island : throw new ArgumentException("Unknown island " + id);
        public EdgeSpec Edge(string id) => id != null && edges.TryGetValue(id, out var edge) ? edge : throw new ArgumentException("Unknown edge " + id);
        public bool HasEdge(string id) => id != null && edges.ContainsKey(id);

        // Bram's route (V1): the finale's edges as (edge, from island, to island), camp first.
        public IReadOnlyList<(EdgeSpec Edge, string From, string To)> FinaleRoute()
        {
            var route = new List<(EdgeSpec, string, string)>(); string at = Camp;
            foreach (var id in Puzzles[5].Edges) { var edge = Edge(id); route.Add((edge, at, edge.Other(at))); at = edge.Other(at); }
            return route.AsReadOnly();
        }

        private void Validate()
        {
            Island(Camp); Island(Sanctuary);
            var kinds = (PuzzleKind[])Enum.GetValues(typeof(PuzzleKind));
            if (Puzzles.Count != kinds.Length) throw new ArgumentException("A valley has exactly six puzzles.");
            for (int i = 0; i < kinds.Length; i++)
            {
                var puzzle = Puzzles[i];
                if (puzzle.Kind != kinds[i]) throw new ArgumentException("Puzzles must run Board, First Bridge, Width, Fork, Arch, Finale.");
                Island(puzzle.TravelerFrom);
                foreach (var id in puzzle.Edges) Edge(id);
                if (puzzle.Kind != PuzzleKind.Finale && puzzle.Edges.Any(id => !Edge(id).Touches(puzzle.TravelerFrom)))
                    throw new ArgumentException(puzzle.Kind + " edges must start at " + puzzle.TravelerFrom + ".");
            }
            if (Puzzles[0].Edges.Count != 1) throw new ArgumentException("Board carries Pip across one edge.");
            if (Puzzles[5].Traveler != TravelerKind.Cart || Puzzles[5].Rule != TravelerKind.Cart || Puzzles[5].TravelerFrom != Camp) throw new ArgumentException("The finale is Bram's cart from the camp.");
            string at = Camp;
            foreach (var id in Puzzles[5].Edges)
            {
                var edge = Edge(id);
                if (!edge.Touches(at)) throw new ArgumentException("The finale route must be a path from the camp.");
                at = edge.Other(at);
            }
            if (at != Sanctuary) throw new ArgumentException("The finale route must end at the sanctuary.");
            if (Ledge != null)
            {
                var edge = Edge(Ledge.Edge);
                if (!edge.Touches(Ledge.Island)) throw new ArgumentException("The ledge must be at one end of its edge.");
                if (!Puzzles[1].Edges.Contains(Ledge.Edge) || !Puzzles[5].Edges.Contains(Ledge.Edge))
                    throw new ArgumentException("The ledge edge is built in First Bridge and rebuilt in the finale (V2).");
            }
        }
    }
}
