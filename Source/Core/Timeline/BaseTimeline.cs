using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniBeat.Lib;

namespace UniBeat.RhythmEngine
{
    /***
    * An abstract timeline that can load data, but cannot be visually represented to screen,
    * and has no listeners to handle events and no interface to access the beat.
    * See DrawableTimeline, TimelineWithPlaybackControl and TimelineTimeProvider for concrete implementations.
    */
    public abstract class BaseTimeline : MonoBehaviour, ITimeline
    {
        public ReactiveProperty<bool> DebugMode { get; private set; } = new ReactiveProperty<bool>(false);
        public bool IsComplete { get; private set; }
        public ReactiveProperty<bool> Active { get; protected set; } = new ReactiveProperty<bool>(false);
        public TimelineRange PlaybackRegion { get; protected set; }
        public bool DataIsLoaded => _data?.Events?.Count > 0;

        // Don't use this, unless you KNOW FOR SURE that data is loaded. Otherwise subscribe to Init.
        public TimelineData Data => _data;

        // Rx ///////////////////////
        // Async Timeline events
        public IObservable<TimelineData> Init => _dataLoaded ? new ReturnObservable<TimelineData>(_data) : _dataSubject.AsObservable();
        public IObservable<ButtonWasPressed> ButtonWasPressed => _inputManager.ButtonWasPressed;
        public IObservable<ButtonWasReleased> ButtonWasReleased => _inputManager.ButtonWasReleased;
        public IObservable<ProgressEvent> Progress => _progress.AsObservable();
        // As the playhead moves through the timeline during gameplay, we emit the events that the playhead encounters.
        public IObservable<ITimelineEvent> GameplayRuntimeEvents => _gameplayRuntimeEventsFlattened.AsObservable();
        public IObservable<T> GameplayRuntimeEventsOfType<T>() where T : ITimelineEvent => GameplayRuntimeEvents.OfType<ITimelineEvent, T>();

        private readonly Subject<ProgressEvent> _progress = new Subject<ProgressEvent>();
        private readonly Subject<IList<ITimelineEvent>> _gameplayRuntimeEvents = new Subject<IList<ITimelineEvent>>();
        private IObservable<ITimelineEvent> _gameplayRuntimeEventsFlattened;


        // Data //////////////////
        protected TimelineData _data;
        protected Subject<TimelineData> _dataSubject = new Subject<TimelineData>();
        private bool _dataLoaded;
        ///////////////////////////

        // Private/Protected members
        protected TimelineConfig _config { get; private set; }
        protected TimelineClock _clock;
        private IInputManager _inputManager => _config.InputManager;

        public InputStyle InputStyle => _config.InputStyle;
        public TimeSpan InputLatency => _config.InputLatency;
        public TimeSpan AudioLatency => _config.AudioLatency;
        public TimeSpan VisualLatency => _config.VisualLatency;

        public async UniTaskVoid SetConfig(TimelineConfig config)
        {
            _config = config;

            await LoadDataAndUpdateObservers();
        }

        private void OnDataLoaded()
        {
            if (_config.PlaybackRegion != null && _config.PlaybackRegion.Size > 0)
            {
                PlaybackRegion = _config.PlaybackRegion;
            }
            else
            {
                PlaybackRegion = new TimelineRange(Math.Floor(_data.Events.First().Index), Math.Ceiling(_data.Events.Last().Index));
            }

            Initialize();
        }

        protected virtual void Initialize()
        {
            _clock = new TimelineClock(PlaybackRegion.Start);
            _clock.SixteenthIndex.Subscribe(index => OnIndexChanged(index));

            // _gameplayRuntimeEvents emits arrays of events at each index, which isn't very useful.
            // we want this flattened into a pure stream of events.
            _gameplayRuntimeEventsFlattened = _gameplayRuntimeEvents.SelectMany<IList<ITimelineEvent>, ITimelineEvent>(events => events.ToObservable());

            SubscribeToTempoEvents();
            SubscribeToUnityHooks();
        }

        private void SubscribeToTempoEvents()
        {
            // Inform the clock immediately of a new BPM.
            Action<TempoEvent> handleTempoEvent = (TempoEvent e) =>
            {
                _clock.UpdateTempoNow(e);
            };

            // Inform the clock that there will be a change of BPM sometime in the future, but before the next index change.
            Action<double> checkForTempoChange = (double index) =>
            {
                var current = _data.TempoAt((int)Math.Min(index, _data.Size.End));
                var next = _data.TempoAt((int)Math.Min(index + 1, _data.Size.End));

                if (next != current)
                {
                    _clock.QueueTempoChange(next);
                }
            };

            // Initially configure clock with the most recent tempo event, relative to our start position.
            handleTempoEvent(_data.MostRecent<TempoEvent>(PlaybackRegion.Start));

            // Handle changes in BPM when we seek to a new position on the timeline
            _clock.OnSeek.Subscribe(index => { handleTempoEvent(_data.MostRecent<TempoEvent>(index)); checkForTempoChange(index); });

            // Handle changes in BPM during ongoing playback of the timeline
            _clock.SixteenthIndex.Subscribe(index => checkForTempoChange(index));
        }

        protected virtual void SubscribeToUnityHooks()
        {
            // // Keep the clock sync'd with Unity Fixed Updates
            this.FixedUpdateAsObservable().Subscribe(_ => _clock.UpdateTimeSample(Time.fixedTimeAsDouble));
        }

        protected virtual void OnIndexChanged(int index)
        {
            EmitEventsForIndex(index);
            EmitProgressEvent(index - 0.5d, index);
        }

        protected void EmitProgressEvent(double oldIndex, double newIndex)
        {
            if (IsComplete)
                return;

            // After we seek, emit any progress events that we skipped over
            if (oldIndex < PlaybackRegion.Start && newIndex >= PlaybackRegion.Start)
            {
                _progress.OnNext(new FirstIndexEvent() { Index = PlaybackRegion.Start });
            }

            if (oldIndex < PlaybackRegion.Middle && newIndex >= PlaybackRegion.Middle)
            {
                _progress.OnNext(new MiddleIndexEvent() { Index = PlaybackRegion.Middle });
            }

            if (oldIndex < PlaybackRegion.End && newIndex >= PlaybackRegion.End)
            {
                _progress.OnNext(new LastIndexEvent() { Index = PlaybackRegion.End });
                _progress.OnCompleted();
                IsComplete = true;
            }
        }

        // Send out gameplay runtime events on every sixteenth index
        private void EmitEventsForIndex(int index)
        {
            var events = _data.EventsAtIndex(index);

            if (events.Count > 0)
            {
                events.ForEach(tEvent => tEvent.OnEmit(index));
                _gameplayRuntimeEvents.OnNext(events);
            }
        }

        protected virtual void OnDisable()
        {
            Active.Value = false;
        }

        protected virtual void OnEnable()
        {

        }

        //////////////////////////////////////////

        protected virtual void OnDestroy()
        {
            _clock.IsPaused = true;
            _clock?.Dispose();
            _gameplayRuntimeEvents.OnCompleted();
            _gameplayRuntimeEvents.Dispose();

            try
            {
                // progress may have already completed and be disposed
                // in which case, this throws
                _progress.OnCompleted();
                _progress.Dispose();
            }
            catch { }
        }


        /////////////////////////////////////
        // Data Loading
        /////////////////////////////////////
        private async UniTask LoadDataAndUpdateObservers()
        {
            _data = await _config.GetTimelineData();
            OnDataLoaded();
            _dataLoaded = true;
            gameObject.name = _data.Id;

            // Notify subscribers
            _dataSubject.OnNext(_data);
            _dataSubject.OnCompleted();
        }
    }
}
