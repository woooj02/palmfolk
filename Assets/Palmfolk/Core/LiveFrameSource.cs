using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    // Live input (D6, T1). The platform samples the hands once per rendered frame (`Sample`); the fixed-tick runner
    // reads one frame per 60 Hz tick (`TryRead`). A tick between two samples holds the latest sample: no
    // extrapolation, so recording the frames this returns and replaying them gives the same decisions (T17a).
    // Requested commands (waystone, system) go out one per tick in order.
    public sealed class LiveFrameSource : IHandFrameSource
    {
        public const int MaximumQueuedCommands = 8;
        private readonly Queue<InputCommand> commands = new Queue<InputCommand>();
        private HandSample left, right;
        private bool focus, controllers;
        private long sampleTime = -1, lastTimestamp = -1;

        public bool HasSample => sampleTime >= 0;
        public long SampleTimestampMicroseconds => sampleTime;
        public int QueuedCommands => commands.Count;
        public HandSample Left => left;
        public HandSample Right => right;

        public void Sample(HandSample leftHand, HandSample rightHand, long timestampMicroseconds, bool hasFocus, bool controllersInHand)
        {
            if (leftHand != null && leftHand.Side != HandSide.Left || rightHand != null && rightHand.Side != HandSide.Right)
                throw new ArgumentException("Hands are on the wrong side.");
            if (timestampMicroseconds < 0) throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds));
            left = leftHand; right = rightHand; focus = hasFocus; controllers = controllersInHand;
            // A clock that steps back (suspend, clock change) never makes frame timestamps go backwards.
            sampleTime = Math.Max(timestampMicroseconds, sampleTime);
        }

        // Queues a command for the next tick. Returns false if too many are already waiting.
        public bool Request(InputCommand command)
        {
            if (command == InputCommand.None) return true;
            if (!Enum.IsDefined(typeof(InputCommand), command)) throw new ArgumentOutOfRangeException(nameof(command));
            if (commands.Count >= MaximumQueuedCommands) return false;
            commands.Enqueue(command); return true;
        }

        public void ClearCommands() => commands.Clear();

        public bool TryRead(long tick, out HandFrame frame)
        {
            frame = null;
            if (!HasSample) return false;
            lastTimestamp = Math.Max(sampleTime, lastTimestamp);
            var command = commands.Count > 0 ? commands.Dequeue() : InputCommand.None;
            frame = new HandFrame(tick, lastTimestamp, left, right, focus, command, controllers);
            return true;
        }
    }
}
