using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace UniBeat.RhythmEngine
{
    [Serializable]
    public class HitSoundSetEvent : BaseTimelineEvent
    {
        // FMOD Studio event paths
        public string HitClap;
        public string HitFinish;
        public string HitNormal;
        public string HitWhistle;
        public string SliderSlide;
        public string SliderTick;

        public IList<String> AllHitSoundEventPaths { get; private set; }
        private Dictionary<HitSoundType, String> _hitSoundByType { get; set; }

        [OnDeserialized]
        internal void OnSerializedMethod(StreamingContext context)
        {
            AllHitSoundEventPaths = new List<String>() {
                HitClap,
                HitFinish,
                HitNormal,
                HitWhistle,
                SliderSlide,
                SliderTick,
            };

            _hitSoundByType = new Dictionary<HitSoundType, String>() {
                { HitSoundType.Normal, HitNormal },
                { HitSoundType.None, HitNormal },
                { HitSoundType.Clap,  HitClap},
                { HitSoundType.Finish,  HitFinish},
                { HitSoundType.Whistle,  HitWhistle}
            };
        }

        public string GetFmodEvent(HitSoundType type)
        {
            return _hitSoundByType[type];
        }
    }
}
