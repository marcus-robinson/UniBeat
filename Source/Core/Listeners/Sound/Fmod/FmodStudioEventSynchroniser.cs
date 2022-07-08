using System;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;

namespace UniBeat.RhythmEngine
{
    public class FmodStudioEventSynchroniser : MonoBehaviour
    {
        public EventDescription EventDescription => _studioEvent.EventDescription;
        private StudioEventEmitter _studioEvent;
        public EventInstance _eventInstance => _studioEvent.EventInstance;
        public bool IsPlaying => _studioEvent.IsPlaying();
        public IObservable<Unit> Ready => _ready.AsObservable();
        private Subject<Unit> _ready = new Subject<Unit>();


        public bool Paused
        {
            get
            {
                bool isPaused;
                _studioEvent.EventInstance.getPaused(out isPaused);
                return isPaused;
            }
            set
            {
                _studioEvent.EventInstance.setPaused(value);
            }
        }

        public void SetStudioEventEmitter(StudioEventEmitter emitter)
        {
            _studioEvent = emitter;
            _studioEvent.Preload = true;
        }

        public void Play()
        {
            _studioEvent.Play();
        }
        public void Stop()
        {
            _studioEvent.Stop();
        }

        public void SetParameter(string name, float value)
        {
            if (!_eventInstance.isValid())
                Debug.LogError($"Unable to set parameter on {_studioEvent.EventReference} because it's not valid.");

            _studioEvent.SetParameter(name, value);
        }

        /// <summary>
        /// Sets the timeline cursor position.
        /// </summary>
        /// <param name="position">Timeline position in milliseconds</param>
        public FMOD.RESULT SetTimelinePosition(int position)
        {
            return _studioEvent.EventInstance.setTimelinePosition(position);
        }

        /// <summary>
        /// Retrieves the timeline cursor position.
        /// </summary>
        /// <returns>Timeline position in milliseconds</returns>
        public int GetTimelinePosition()
        {
            int position;
            _eventInstance.getTimelinePosition(out position);
            return position;
        }

        private int? _lengthCache;
        public TimeSpan GetLength()
        {
            int length;
            // Cache to reduce the # of calls out to FMOD DLL, since it's a bit flaky under load.
            if (_lengthCache == null)
            {
                EventDescription.getLength(out length);
                _lengthCache = length;
            }
            return TimeSpan.FromMilliseconds((int)_lengthCache);
        }

        // Force preloading and force event into memory by playing it and immediately pausing it.
        private void Awake()
        {
            if (_studioEvent == null)
            {
                _studioEvent = GetComponent<StudioEventEmitter>();
                _studioEvent.Preload = true;
            }
            if (_studioEvent == null)
            {
                UnityEngine.Debug.LogWarning("Preloader must have a StudioEventEmitter defined.");
            }

            // Wait until we've had a fixed update. Not sure why we need this, but without it,
            // the FMOD event never plays, and MonitorLoadingState just watches forever...
            this.FixedUpdateAsObservable().First().Subscribe(_ =>
            {
                // play the event to force the audio into memory and into the DSP buffer
                // This is critical to ensure latency is as low as possible.
                _studioEvent.Play();
                Paused = true;
                MonitorLoadingState().Forget();
            });
        }

        private async UniTask MonitorLoadingState()
        {
            LOADING_STATE state;
            await UniTask.WaitUntil(() =>
            {
                EventDescription.getSampleLoadingState(out state);
                return state == LOADING_STATE.LOADED && _eventInstance.isValid();
            });

            // UnityEngine.Debug.Log($"::FMOD:: WE ARE LOADED? {_studioEvent.EventReference.Path}");

            _ready.OnNext(Unit.Default);
            _ready.OnCompleted();
        }
    }
}
