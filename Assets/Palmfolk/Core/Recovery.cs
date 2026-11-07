using System;

namespace Palmfolk.Core
{
    public enum RecoveryPhase { Frozen, Aligning, Blending, Returning, Recovered, Returned }
    public sealed class RecoverySnapshot
    {
        public Route Ghost;
        public Vec3 Position, Frozen, ReturnFrom;
        public RecoveryPhase Phase;
        public int Elapsed, Aligned, Blending, Returning;
    }
    public sealed class Recovery
    {
        private readonly GameSettings settings;
        private readonly Vec3 checkpoint;
        private Vec3 frozen, returnFrom;
        private int elapsed, aligned, blending, returning;
        public Route Ghost { get; private set; }
        public RecoveryPhase Phase { get; private set; } = RecoveryPhase.Frozen;
        public Vec3 Position { get; private set; }
        public Recovery(Route ghost, Vec3 position, Vec3 checkpoint, GameSettings settings)
        {
            Ghost = ghost ?? throw new ArgumentNullException(nameof(ghost)); frozen = Position = position;
            this.checkpoint = checkpoint; this.settings = settings.Copy();
        }
        public void ReturnToCheckpoint()
        {
            if (Phase == RecoveryPhase.Returning || Phase == RecoveryPhase.Returned) return;
            returnFrom = Position; returning = 0; Phase = RecoveryPhase.Returning;
        }
        public void Step(Route ownedLiveRoute, Vec3 livePosition)
        {
            if (Phase == RecoveryPhase.Returned || Phase == RecoveryPhase.Recovered) return;
            if (Phase == RecoveryPhase.Returning)
            {
                Position = Vec3.Lerp(returnFrom, checkpoint, (double)++returning / settings.ReturnTicks);
                if (returning >= settings.ReturnTicks) Phase = RecoveryPhase.Returned;
                return;
            }
            elapsed++;
            bool close = ownedLiveRoute != null && Ghost.Difference(ownedLiveRoute) < settings.ConnectDistance;
            if (Phase == RecoveryPhase.Blending)
            {
                if (!close) { frozen = Position; aligned = blending = 0; Phase = RecoveryPhase.Frozen; }
                else
                {
                    Position = Vec3.Lerp(frozen, livePosition, (double)++blending / settings.BlendTicks);
                    if (blending >= settings.BlendTicks) Phase = RecoveryPhase.Recovered;
                    return;
                }
            }
            if (close)
            {
                Phase = RecoveryPhase.Aligning;
                if (++aligned >= settings.AlignmentTicks) { Phase = RecoveryPhase.Blending; blending = 0; }
            }
            else { aligned = 0; Phase = RecoveryPhase.Frozen; }
            if (elapsed >= settings.RecoveryTicks && Phase != RecoveryPhase.Blending) ReturnToCheckpoint();
        }
        public RecoverySnapshot Capture() => new RecoverySnapshot { Ghost = Ghost, Position = Position, Frozen = frozen, ReturnFrom = returnFrom, Phase = Phase,
            Elapsed = elapsed, Aligned = aligned, Blending = blending, Returning = returning };
        public static Recovery Restore(RecoverySnapshot s, Vec3 checkpoint, GameSettings settings)
        {
            if (s == null || s.Ghost == null || !s.Position.IsFinite || !s.Frozen.IsFinite || !s.ReturnFrom.IsFinite || !Enum.IsDefined(typeof(RecoveryPhase), s.Phase) ||
                s.Elapsed < 0 || s.Aligned < 0 || s.Aligned > settings.AlignmentTicks || s.Blending < 0 || s.Blending > settings.BlendTicks || s.Returning < 0 || s.Returning > settings.ReturnTicks)
                throw new ArgumentException("Invalid recovery snapshot.");
            return new Recovery(s.Ghost, s.Position, checkpoint, settings) { Phase = s.Phase, frozen = s.Frozen, returnFrom = s.ReturnFrom,
                elapsed = s.Elapsed, aligned = s.Aligned, blending = s.Blending, returning = s.Returning };
        }
    }
}
