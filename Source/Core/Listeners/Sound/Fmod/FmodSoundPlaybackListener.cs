using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Cysharp.Threading.Tasks;

namespace UniBeat.RhythmEngine
{
    public class FmodSoundPlaybackListener<T> : BaseFmodListener<T>
        where T : SoundEvent
    {
        protected readonly Dictionary<T, FmodStudioEventSynchroniser> _eventMap = new Dictionary<T, FmodStudioEventSynchroniser>();

        public FmodSoundPlaybackListener(ITimelineTimeProvider timeline) : base(timeline) { }

        protected async override UniTask Init(TimelineData data)
        {
            await GenerateSynchronizers(data);

            foreach (KeyValuePair<T, FmodStudioEventSynchroniser> entry in _eventMap)
            {
                if (entry.Key.Params != null)
                {
                    foreach (KeyValuePair<string, float> param in entry.Key.Params)
                    {
                        entry.Value.SetParameter(param.Key, param.Value);
                    }
                }
            }
        }

        protected override void OnEvent(T soundEvent)
        {
            if (soundEvent.GetType() != typeof(T))
            {
                UnityEngine.Debug.Log($"{soundEvent} is being ignored by {GetType().Name} because it's a parent class of {typeof(T)}. It must be the exact type of T.");
                return;
            }

            UnityEngine.Debug.Log($"AudioPlaybackListener.OnEvent{soundEvent}");
            FmodStudioEventSynchroniser fmod;
            _eventMap.TryGetValue(soundEvent, out fmod);

            fmod?.Play();
        }

        // For each fmod event in this timeline, load it into memory via Synchronizers.
        // Synchronizer will notify us when the event is ready.
        // Important so that is instantly plays and we'll have crucial information like track length
        // at the point in time when the timeline plays.
        private UniTask GenerateSynchronizers(TimelineData data)
        {
            // OfType will pick up all the exotic audio events that extend from T, so for example, if T is SoundEvent, OfType willl also pick up MusicEvent
            // so we need to add the strict GetType() == typeof(T) check
            var syncObserve = data.Events.FindAll(e => e.GetType() == typeof(T)).Cast<T>().Select<T, IObservable<Unit>>(e =>
            {
                var sync = CreateSynchoniser(e);
                _eventMap.Add(e, sync);
                return sync.Ready;
            });

            return Observable.WhenAll(syncObserve).ToUniTask();
        }

        //TODO - if e.key != null, load .mp3 via Asset Manager
        private FmodStudioEventSynchroniser CreateSynchoniser(T sEvent)
        {
            return CreateSynchoniser(sEvent.Asset);
        }

    }
}
