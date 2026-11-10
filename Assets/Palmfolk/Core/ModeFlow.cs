using System;

namespace Palmfolk.Core
{
    public enum AnchorSearch { None, Finding, Found, Lost }

    // D9 (MR3, MR5): which mode a launch runs in and where the valley starts. The platform reports what passthrough
    // and spatial anchors do; this decides, so the decisions are tested headless.
    //
    // - VR mode, or Table mode without passthrough (not supported, not permitted, or failed): VR runs, at the saved
    //   pose if there is one, otherwise the player plants. The player's choice (`Chosen`) is kept for next time.
    // - Table mode with a saved anchor: the platform looks for it for up to five seconds. Found: the valley opens at
    //   the anchor, at the saved scale. Not found: the player replants (bridges are valley-local, so none are lost).
    // - Table mode without an anchor: the player plants. Each planting in Table mode saves a new anchor and the old
    //   one is erased. If saving fails, the next launch asks to replant.
    public sealed class ModeFlow
    {
        public const double FindSeconds = 5;
        private readonly double scale;
        private double waited;

        public ValleyMode Chosen { get; private set; }
        public ValleyMode Running { get; private set; }
        public bool FellBack => Chosen == ValleyMode.Table && Running == ValleyMode.VR;
        public bool Passthrough => Running == ValleyMode.Table;
        public string AnchorId { get; private set; }
        public AnchorSearch Search { get; private set; }
        // Decided: the game can open, at StartPose (null: the player plants).
        public bool Decided { get; private set; }
        public ValleyPose StartPose { get; private set; }
        public double FindProgress => Search == AnchorSearch.Finding ? Math.Min(1, waited / FindSeconds) : Search == AnchorSearch.None ? 0 : 1;

        public ModeFlow(SaveData save, bool passthroughAvailable)
        {
            save = save ?? new SaveData();
            Chosen = save.Mode; AnchorId = save.AnchorId; scale = save.Pose?.Scale ?? 1;
            Running = Chosen == ValleyMode.Table && !passthroughAvailable ? ValleyMode.VR : Chosen;
            if (Running == ValleyMode.VR) { StartPose = save.Pose; Decided = true; return; }
            if (AnchorId == null) { Decided = true; return; }
            Search = AnchorSearch.Finding;
        }

        // The platform found the anchor (world space). Ignored once the search is over.
        public bool AnchorFound(Vec3 position, Vec3 forward)
        {
            if (Search != AnchorSearch.Finding) return false;
            if (!position.IsFinite || !forward.IsFinite || new Vec3(forward.X, 0, forward.Z).Length < 1e-6) { Lose(); return false; }
            Search = AnchorSearch.Found; StartPose = new ValleyPose(position, forward, scale); Decided = true;
            return true;
        }

        // The platform knows the anchor can't be found (unknown id, erased, failed to localize).
        public void AnchorMissing() { if (Search == AnchorSearch.Finding) Lose(); }

        // Real time spent waiting for the anchor.
        public void Wait(double seconds)
        {
            if (Search != AnchorSearch.Finding) return;
            if (seconds > 0 && !double.IsNaN(seconds)) waited += seconds;
            if (waited >= FindSeconds) Lose();
        }

        private void Lose() { Search = AnchorSearch.Lost; StartPose = null; Decided = true; }

        // Planted (or the valley moved into Table mode) while running Table mode: the old anchor no longer marks the
        // valley. Returns it for the platform to erase; the new one arrives through Anchored.
        public string Replace()
        {
            var old = AnchorId; AnchorId = null; return old;
        }

        public void Anchored(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 128) throw new ArgumentException("Invalid anchor id.", nameof(id));
            AnchorId = id;
        }

        // Passthrough stopped mid-game: VR carries on with the valley where it is.
        public bool PassthroughLost()
        {
            if (Running != ValleyMode.Table) return false;
            Running = ValleyMode.VR; return true;
        }

        // The waystone's choice (MR5). Returns whether the running mode changed.
        public bool Choose(ValleyMode mode, bool passthroughAvailable)
        {
            if (!Enum.IsDefined(typeof(ValleyMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            var before = Running; Chosen = mode;
            Running = mode == ValleyMode.Table && !passthroughAvailable ? ValleyMode.VR : mode;
            return Running != before;
        }

        // Writes the chosen mode and the anchor into a save.
        public SaveData Into(SaveData save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            save.Mode = Chosen; save.AnchorId = AnchorId; return save;
        }
    }
}
