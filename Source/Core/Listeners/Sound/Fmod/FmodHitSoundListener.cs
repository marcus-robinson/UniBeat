using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using UniBeat.Lib;
using Cysharp.Threading.Tasks;

namespace UniBeat.RhythmEngine
{
    public abstract class FmodHitSoundListener : BaseFmodListener<HitSoundSetEvent>
    {
        protected Dictionary<HitSoundType, FmodStudioEventSynchroniser> _eventsByHitSoundType;
        private HitSoundSetEvent _hitSoundSet;

        public FmodHitSoundListener(ITimelineTimeProvider timeline) : base(timeline) { }

        protected override UniTask Init(TimelineData data)
        {
            var mostRecent = data.MostRecent<HitSoundSetEvent>(_timeline.PlaybackRegion.Start);

            if (mostRecent == null)
            {
                // This might be too dramatic - we could theoretically look for hitsounds into the future and use those...
                UnityEngine.Debug.LogError($"This timeline doesn't have any hit sounds at or before the start of the playback regions. Self-destructing now.");
                Dispose();
            }
            else
            {
                // Iniitalise by setting our hit sounds using the most recent hit sound event in the timeline
                OnEvent(mostRecent);
            }

            return UniTask.CompletedTask;
        }

        protected override void StartListening()
        {
            base.StartListening();

            if (_timeline.InputStyle == InputStyle.TriggerHitOnButtonPressed)
            {
                _timeline.ButtonWasPressed.Subscribe(e => OnButtonPress(e)).AddTo(_subscriptions);
            }
            else
            {
                _timeline.ButtonWasReleased.Subscribe(e => OnButtonPress(e)).AddTo(_subscriptions);
            }
        }

        protected override void OnEvent(HitSoundSetEvent hitSoundSet)
        {
            if (hitSoundSet == null)
                return;

            _hitSoundSet = hitSoundSet;
            PopulateHitSoundMap();
        }

        private void PopulateHitSoundMap()
        {
            _eventsByHitSoundType = new Dictionary<HitSoundType, FmodStudioEventSynchroniser>();

            var hitsounds = Enum.GetValues(typeof(HitSoundType)).Cast<HitSoundType>().ToList();
            hitsounds?.ForEach(hitsound =>
            {
                var path = _hitSoundSet.GetFmodEvent(hitsound);
                _eventsByHitSoundType.Add(hitsound, CreateSynchoniser(path));
            });
        }

        protected abstract void OnButtonPress(ButtonEvent e);

    }
}
