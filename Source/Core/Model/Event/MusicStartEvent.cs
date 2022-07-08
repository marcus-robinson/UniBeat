using System;

namespace UniBeat.RhythmEngine
{
    // Start some music playing
    [Serializable]
    public class MusicStartEvent : SoundEvent
    {
        public bool Loop;
    }
}
