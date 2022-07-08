using System;
using System.Collections.Generic;

namespace UniBeat.RhythmEngine
{
    [Serializable]
    public class SoundEvent : BaseTimelineEvent
    {
        // The path to the asset. Will be in different formats depending on Timeline's listeners.
        // Could be an Addressable Asset Key or maybe a FMOD Studio event path.
        public string Asset;

        // Additional data about the sound.
        // How these are used depends on the timeline's listeners.
        // If FMOD listeners are being used, these params are set on the generated Studio Event Instance.
        public Dictionary<string, float> Params;
    }
}
