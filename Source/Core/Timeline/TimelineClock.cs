using System;
using System.Numerics;
using UniRx;
using UnityEditor;
using UnityEngine;

namespace UniBeat.RhythmEngine
{
    public class TimelineClock : IDisposable
    {
        public IObservable<int> BeatIndex => _beatIndex.AsObservable(); //Fires on the beat
        public IObservable<int> SixteenthIndex => _sixteenthIndex.AsObservable(); //Fires on sixteenths
        public IObservable<double> OnSeek => _seek.AsObservable(); //Fires when we seek to a new position on the timeline
        public IObservable<bool> Paused => _pause.AsObservable();
        public IObservable<Unit> Reset => _reset.AsObservable();
        public TimeSignature TimeSignature { get; private set; } = TimeSignature.CommonTime;
        public Tempo Tempo { get; private set; } = Tempo.Allegro;

        ///////////////////////////////////////////////////////////////////////
        // CURRENT STATE

        // How far in are we in beats.
        // Beat index is a double, updated every frame.
        // When the double value is at an integer point (e.g 5.000), then we're "on the beat".
        // Otherwise, we're inbetween beats.
        public double BeatIndexValue => _beatTime;

        // How far in are we in sixteenths
        public double SixteenthIndexValue => _beatTime * Utils.SixteenthsPerBeat;
        ///////////////////////////////////////////////////////////////////////


        // When you convert a double or float value to an integral type,
        // this value is rounded towards zero to the nearest integral value.
        public int PreviousSixteenthIndexValue => (int)SixteenthIndexValue;
        public int NextSixteenthIndexValue => (int)SixteenthIndexValue + 1;

        // The duration of a single beat in seconds for the current BPM
        public TimeSpan BeatTimeSpan => Tempo.BeatTimeSpan;

        // The duration of a single sixteenth note in seconds for the current BPM
        public TimeSpan SixteenthTimeSpan => TimeSpan.FromSeconds(BeatTimeSpan.TotalSeconds / Utils.SixteenthsPerBeat);

        private TempoEvent _pendingEvent;

        private bool _isPaused = true;
        public bool IsPaused
        {
            get
            {
                return _isPaused;
            }
            set
            {
                if (_isPaused == value)
                    return;
                //Debug.Log($"new pause value is: {value}");
                _isPaused = value;

                _pause.OnNext(_isPaused);

                // If we're playing, trigger onNext's for starting index before we start playing.
                // Without this, the "_startBeat" beat/index is never emitted.
                if (!_isPaused && _beatTime == _startBeat)
                    UpdateSubjects(true);
            }
        }

        public double Bpm => Tempo.BeatsPerMinute;

        public TimelineClock(double startingIndex = 0d)
        {
            _startBeat = startingIndex / Utils.SixteenthsPerBeat;
            _beatTime = _startBeat;// - SYNC_WINDOW;
        }

        /// <summary>
        /// Immediately update the time signature and tempo
        /// </summary>
        public void UpdateTempoNow(TempoEvent e)
        {
            TimeSignature = e.TimeSignature;
            Tempo = e.Tempo;
            _pendingEvent = null;
        }

        /// <summary>
        /// Update the time signature and tempo when we encounter this event's index
        /// </summary>
        public void QueueTempoChange(TempoEvent e)
        {
            _pendingEvent = e;
        }

        /// <summary>
        /// Resets beat time from the beginning.
        /// </summary>
        public void DoReset(double time)
        {
            _beatTime = _startBeat;// - SYNC_WINDOW;
            _lastSampledTime = time;
            _reset.OnNext(Unit.Default);
        }

        public void UpdateTimeSample(double time)
        {
            if (_isPaused)
            {
                _lastSampledTime = time;
                return;
            }

            double deltaTime = time - _lastSampledTime;
            _lastSampledTime = time;

            _beatTime += deltaTime * Bpm / SECONDS_PER_MIN;

            if (_pendingEvent != null && SixteenthIndexValue >= _pendingEvent.IndexRelativeToPriorBPM)
            {
                // At this point, we may be as much as a full FixedTimeStep PAST this new beat time (5ms as configured in Player settings)
                // so we can't just set the current index to be exactly equal to the pending tempo event's index.
                var indexOverrun = SixteenthIndexValue - _pendingEvent.IndexRelativeToPriorBPM;
                var timeOverrun = (indexOverrun / Utils.SixteenthsPerBeat) * SECONDS_PER_MIN / Bpm; // seconds
                var beatOverrun = timeOverrun * _pendingEvent.Tempo.BeatsPerMinute / SECONDS_PER_MIN; //beats

                // Debug.Log($"clock index is {SixteenthIndexValue} @ {Bpm}bpm, but we are now going to fast forward it to the new {_pendingEvent.Tempo.BeatsPerMinute}BPM event's index of {_pendingEvent.Index} + an overrun of {(int)Math.Round(timeOverrun * 1000)}ms because pending music event has IndexRelativeToPriorBPM of: {_pendingEvent.IndexRelativeToPriorBPM}.");

                _beatTime = _pendingEvent.Index / Utils.SixteenthsPerBeat + beatOverrun;

                UpdateTempoNow(_pendingEvent);
                UpdateSubjects(true);
            }
            else
            {
                UpdateSubjects();
            }
        }

        /// <summary>
        /// Sets the timeline clock to a specific sixteenth index.
        /// </summary>
        /// <param name="index">The timeline position to seek to, prvovided in sixteenths as an absolute position.</param>
        public void SeekTo(double newStartPos, double time)
        {
            if (newStartPos < 0d)
            {
                Debug.LogError($"{newStartPos} is an invalid timeline position. Value must be > 0.");
                return;
            }

            var newBeatTime = newStartPos / Utils.SixteenthsPerBeat;
            // Debug.Log($"newBeatTime: {newBeatTime}");

            _beatTime = newBeatTime;
            _lastSampledTime = time;
            _counter = PreviousSixteenthIndexValue;

            _seek.OnNext(SixteenthIndexValue);
        }

        public void Dispose()
        {
            _seek.OnCompleted();
            _seek.Dispose();
            _beatIndex.OnCompleted();
            _beatIndex.Dispose();
            _sixteenthIndex.OnCompleted();
            _sixteenthIndex.Dispose();
            _pause.OnCompleted();
            _pause.Dispose();
            _reset.OnCompleted();
            _reset.Dispose();
        }

        private void UpdateSubjects(bool force = false)
        {
            if (_counter != PreviousSixteenthIndexValue || force)
            {
                _counter = PreviousSixteenthIndexValue;
                _sixteenthIndex.OnNext(PreviousSixteenthIndexValue);

                if (PreviousSixteenthIndexValue % TimeSignature.TypeOfBeats == 0)
                {
                    //Debug.Log($" beat index: {(int)BeatIndex} || Sixteenth index: {BeatIndexInSixteenths} || Track position: {(_studioEvents.Count > 0 ? _studioEvents[0]?.GetTimelinePosition().ToString() : "n/a")}ms");
                    _beatIndex.OnNext((int)BeatIndexValue);
                }
            }
        }

        private const double SECONDS_PER_MIN = 60d;

        // When the clock inits or resets, start things this far behind the intended start beat.
        // This sync window is functionally useless now, since everything is mostly controlled with "seek" calls.
        // So when we start the clock, we don't just go "Play!" - we go "Seek to 0, then play".
        // which means the sync window is always ignored.
        //private const double SYNC_WINDOW = 1 / 16d; // beats
        private readonly double _startBeat;
        private double _beatTime;
        private double _counter;
        private double _lastSampledTime = 0d;
        private readonly Subject<double> _seek = new Subject<double>();
        private readonly Subject<int> _beatIndex = new Subject<int>();
        private readonly Subject<int> _sixteenthIndex = new Subject<int>();
        private readonly Subject<bool> _pause = new Subject<bool>();
        /// A null reset event is triggered when time restarts.
        private readonly Subject<Unit> _reset = new Subject<Unit>();
    }
}
