using System;
using System.Collections.Generic;
using UniBeat.Lib;
using UniRx;
using UnityEngine;
namespace UniBeat.RhythmEngine
{
    // Similar to Sound Playback, except that you can only have one music track playing at any given time.
    public class FmodMusicPlaybackListener : FmodSoundPlaybackListener<MusicStartEvent>
    {
        private FmodStudioEventSynchroniser _currentlyPlaying;
        private MusicStartEvent _currentEvent;
        private TimeSpan _currentlyPlayingLength => _currentlyPlaying.GetLength();
        private bool _hasMusic => _currentlyPlaying != null;

        // Auto pause the song if we get within this many beats of the end because FMOD is a baby.
        private const int _autoPauseThreshold = 2;

        public bool Paused
        {
            get
            {
                return _currentlyPlaying == null || _currentlyPlaying.Paused;
            }
            private set
            {
                // short circuit if the pause value hasn't changed
                if (_hasMusic && _currentlyPlaying.Paused == value)
                    return;

                // If we're about to unpause
                if (!value)
                {
                    // First be sure the timeline is correctly positioned.
                    // Will return false if we're requesting an impossible state.
                    var isValid = SetTimelinePosition();

                    if (!isValid)
                    {
                        // Don't play the event if we've requested an invalid state.
                        return;
                    }
                }

                if (_hasMusic)
                    _currentlyPlaying.Paused = value;
            }
        }

        public FmodMusicPlaybackListener(ITimelineTimeProvider timeline) : base(timeline) { }

        protected override void StartListening()
        {
            base.StartListening();

            SetTimelinePosition();

            _timeline.Paused.Subscribe(paused =>
            {
                Paused = paused;
            }).AddTo(_subscriptions);

            _timeline.BeatIndex.Subscribe(beat =>
            {
                Paused = TimelineHasPassedEndOfSong();
                SampleLatency();
            }).AddTo(_subscriptions);

            _timeline.OnSeek.Subscribe(index =>
            {
                Paused = _timeline.IsPaused;
            }).AddTo(_subscriptions);

            _timeline.Progress.OfType<ProgressEvent, LastIndexEvent>().Subscribe(_ => CalculateLatencyStats());
        }

        // If we come across a music event on the timeline, play .. but only if the timeline is playing!
        // In most scenarios, we were emitted because the timeline was playing and we came across this event,
        // However, we may have been emitted because we called seek and went passed it and the timeline is actually paused.
        protected override void OnEvent(MusicStartEvent e)
        {
            Paused = _timeline.IsPaused;
        }

        protected override void StopListening()
        {
            base.StopListening();

            // Stop the music playing when we stop listening.
            Paused = true;
        }

        // Set the timeline position of the FMOD Event to the desired position
        // Note, this method has nothing to do with playing / pausing - it's only
        // positioning the playhead based on the current state of the timeline.
        private bool SetTimelinePosition()
        {
            Debug.Log("SetTimelinePosition");

            _currentEvent = _timeline.Data.MostRecent<MusicStartEvent>(_timeline.CurrentIndex);

            // If there's nothing in the timeline to play at this index.
            if (_currentEvent == null)
            {
                Debug.Log("there's nothing in the timeline to play at this index");

                Paused = true; // before we null out _currentlyPlaying, pause it otherwise it keeps playing after we lose reference.
                _currentlyPlaying = null;
                return false;
            }

            // Find the FMOD event emitter in our cache
            _eventMap.TryGetValue(_currentEvent, out _currentlyPlaying);

            if (_currentlyPlaying == null)
                Debug.Log("_currentlyPlaying == null");


            if (_currentlyPlaying == null)
                return false;

            var fmodPos = _timeline.TimeOf(_timeline.CurrentIndex) - _timeline.TimeOf(_currentEvent.Index) + _timeline.AudioLatency;

            if (fmodPos >= _currentlyPlayingLength)
            {
                Debug.LogWarning($"FMOD :: Requested FMOD timeline pos of {fmodPos} is invalid because it's greater than the track length of {_currentlyPlayingLength}.");
                return false;
            }

            if (TimelineHasPassedEndOfSong())
            {
                Debug.LogWarning($"FMOD :: Requested FMOD timeline pos of {fmodPos} is valid, but we're ignoring it because we're within {_autoPauseThreshold} beats of the end of the song.");
                return false;
            }

            // Sync the FMOD event's timeline position with our ITimelineTimeProvider's position
            Debug.Log($"FMOD :: SetTimeline({(int)fmodPos.TotalMilliseconds}) || {fmodPos} || Because this song starts at  {_timeline.TimeOf(_currentEvent.Index)} and we're currently at timeline index: {_timeline.CurrentIndex} which has a time of: {_timeline.TimeOf(_timeline.CurrentIndex)}");
            var result = _currentlyPlaying.SetTimelinePosition((int)fmodPos.TotalMilliseconds);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"FMOD :: Unable to set timeline position because: {result.ToString()}");
                return false;
            }
            return true;
        }

        // This is called on event BEAT. Pause the song before we overrun.
        // FMOD is a sensitive baby that fails silently if the song plays all the way to the end.
        // This is in place because after playing past the end of bohemian rhapsody, the music just stops playing.
        // i.e When you run back into a valid part of the timeline - it's like the event is corrupted, even though isValid return true,
        // the music just doesn't play anymore.
        protected bool TimelineHasPassedEndOfSong()
        {
            if (_currentlyPlaying == null)
            {
                return false;
            }

            var startTimeOfCurrentSong = _timeline.TimeOf(_currentEvent.Index);
            var indexThatThisSongEndsOn = _timeline.IndexOf(startTimeOfCurrentSong + _currentlyPlayingLength);

            // Stop the track 2 beats before the end
            // This is because we only run this check on every beat and we need some buffer to handle end-of-song edge cases.
            var endOfSongThreshold = indexThatThisSongEndsOn - _autoPauseThreshold * Utils.SixteenthsPerBeat;
            if (_timeline.CurrentIndex >= endOfSongThreshold)
            {
                // Debug.LogWarning($"%%$% FLYING TOO CLOSE TO THE SUN - PAUSED FOR SAFETY #$%#$%#$%#$%#$ startTimeOfCurrentSong: {startTimeOfCurrentSong} | _currentlyPlayingLength: {_currentlyPlayingLength} | indexThatThisSongEndsOn: {indexThatThisSongEndsOn} | currentIndex: {currentIndex}");
                return true;
            }
            else
            {
                // Debug.Log($"Safe - {currentIndex} is less than the song's end index of {indexThatThisSongEndsOn}");
                return false;
            }
        }

        private List<double> _samples = new List<double>();
        private void CalculateLatencyStats()
        {
            if (_samples.Count == 0)
                return;

            var mean = Calc.Mean(_samples.ToArray());
            var median = Calc.Median(_samples.ToArray());
            var stdDev = Calc.StandardDeviation(_samples.ToArray());
            var count = _samples.Count;
            _samples.Clear();

            Debug.Log(@$"
            FMOD LATENCY STATS based on {count} samples:
            Mean: {mean.ToString("F2")}, Median: {median.ToString("F2")}, Std Dev: {stdDev.ToString("F2")}");
        }

        private void SampleLatency()
        {
            if (!_hasMusic)
                return;

            var timelineTime = _timeline.TimeOf(_timeline.CurrentIndex) - _timeline.TimeOf(_currentEvent.Index);
            var fmodTime = TimeSpan.FromMilliseconds(_currentlyPlaying.GetTimelinePosition());
            _samples.Add((timelineTime - fmodTime).TotalMilliseconds);
        }
    }
}
