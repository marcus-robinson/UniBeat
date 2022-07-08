using System.Collections.Generic;

namespace UniBeat.RhythmEngine
{
    public class Replay
    {
        /// <summary>
        /// Whether all frames for this replay have been received.
        /// If false, gameplay would be paused to wait for further data, for instance.
        /// </summary>
        public bool HasReceivedAllFrames = true;

        public List<ReplayFrame> Frames = new List<ReplayFrame>();
    }
}
