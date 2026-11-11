using System;
using Palmfolk.Core;

namespace Palmfolk.Platform
{
    // D9: spatial anchors (MR3). Positions and forwards are Unity world space. Callbacks come on the main thread,
    // possibly before the call returns.
    public interface ISpatialAnchors
    {
        // Saves a new anchor at a pose; `done` gets its id, or null if it couldn't be saved.
        void Save(Vec3 position, Vec3 forward, Action<string> done);
        // Finds a saved anchor; `done` gets true and its pose, or false when it can't be found.
        void Find(string id, Action<bool, Vec3, Vec3> done);
        void Erase(string id);
    }

    // D10 (GZ3): the gaze ray this frame in world space, or null when there is none. Source says whether it is
    // eye-tracked or synthesized from the head pose.
    public interface IGazeReader
    {
        GazeSample Read();
    }

    // D9: passthrough (MR1, MR5). Available: supported, permitted and not failed.
    public interface IPassthrough
    {
        bool Available { get; }
        bool Visible { get; }
        void SetVisible(bool visible);
    }
}
