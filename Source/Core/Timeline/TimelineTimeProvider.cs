using System;
using System.Linq;
using UniRx;
using UnityEngine;
namespace UniBeat.RhythmEngine
{
    /***
    * Add ability to access the beat state of the timeline
    */
    public class TimelineTimeProvider : BaseTimeline, ITimelineTimeProvider
    {
        // Current State //////////////
        // These values may change based on where we are in the timeline.
        public TimeSpan CurrentSixteenthDuration => _clock.SixteenthTimeSpan;
        public TimeSpan CurrentBeatDuration => _clock.BeatTimeSpan;
        public double CurrentIndex => _clock.SixteenthIndexValue;
        public double CurrentBeat => _clock.BeatIndexValue;
        public Tempo CurrentTempo => _clock.Tempo;
        public TimeSpan CurrentTime => TimeOf(CurrentIndex);
        public int NextIndex => Mathf.Min(_clock.NextSixteenthIndexValue, PlaybackRegion.EndAsInt);
        public int PreviousIndex => Mathf.Max(_clock.PreviousSixteenthIndexValue, PlaybackRegion.StartAsInt);
        public bool IsPaused => _clock.IsPaused;
        public virtual bool AtFinalIndex => _clock.SixteenthIndexValue >= PlaybackRegion.End || Mathf.Approximately((float)_clock.SixteenthIndexValue, (float)PlaybackRegion.End);


        // Rx ///////////////////////
        // Async Timeline events
        public IObservable<int> BeatIndex => _clock.BeatIndex;
        public IObservable<int> SixteenthIndex => _clock.SixteenthIndex;
        public IObservable<bool> Paused => _clock.Paused;
        public IObservable<double> OnSeek => _clock.OnSeek;

        // Return the sixteenth index at the time provided.
        // Keep in mind that this may return an index value higher the bounds of timeline if the
        // provided time goes outside the timeline limits.
        public double IndexOf(TimeSpan time)
        {
            return _data.Events.OfType<TempoEvent>().Last(e => e.Offset <= time).IndexOf(time);
        }

        // Return the clock time for a given sixteenth index on the timeline
        public TimeSpan TimeOf(double index)
        {
            if (index == 0d)
                return TimeSpan.Zero;

            return _data.TempoAt((int)Math.Round(index)).TimeOf(index);

            // // Add +1 to handle weird floating point edge cases where index might be 22.5001 and our end is 22.5
            // double finalIndex = Math.Max(_data.Size.End, PlaybackRegion.EndAsInt) + 1;

            // return _data.Events.OfType<TempoEvent>().First(e => e.Index <= index && (e.EndIndex ?? finalIndex) >= index).TimeOf(index);
        }
    }
}
