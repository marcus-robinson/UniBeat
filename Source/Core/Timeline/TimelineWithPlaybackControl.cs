using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace UniBeat.RhythmEngine
{
    /***
    * Add ability to control the playback of the timeline with Play / Pause / Seek operations.
    */
    public class TimelineWithPlaybackControl : TimelineTimeProvider, ITimelinePlaybackControl
    {
        // PLAYBACK METHODS //////////////////////
        public void SeekTo(double newIndex)
        {
            // If we're at the start or end of the timeline, but a tiny bit shy of the PlaybackRegion's start/end, then,
            // thanks for floating point magic, we may get valid newIndex values that are a fractional of a decimal outside the allowed bounds.
            // For example, when we exit a timeline, the index may be something like 199.9 and PlaybackRegion.End is 200
            // and there could be events right bang on 200 that will not be emitted in the upcoming onEmit calls that we make,
            // unless we adjust. Or we may have a timeline that starts at 6, but the very first seekTo is 5.99999.
            if (!PlaybackRegion.Contains(newIndex))
            {
                Debug.LogWarning($"{newIndex.ToString("F2")} is outside this Timeline's range, so we're clamping it to a valid value of {PlaybackRegion.Clamp(newIndex)}");
                newIndex = PlaybackRegion.Clamp(newIndex);
            }

            // Need to cache, since this value is updated after the clock seeks.
            var cacheCurrentIndex = CurrentIndex;

            // Debug.Log($"Timeline is seeking to {newIndex.ToString("F2")}");
            _clock.SeekTo(newIndex, Time.fixedTimeAsDouble);

            if (newIndex < cacheCurrentIndex)
                return; // if we're seeking backwards, don't emit any in-between events

            // After we seek, emit all the events that we skipped past.
            // For example, this ensures that hitobject events that were skipped receive a 'miss' score.
            _data.InRange(new TimelineRange(cacheCurrentIndex, newIndex)).ToList().ForEach(e =>
            {
                e.OnEmit(newIndex);
            });

            EmitProgressEvent(cacheCurrentIndex, newIndex);
        }

        public void Play()
        {
            ThrowIfInactive();
            if (_clock.SixteenthIndexValue >= PlaybackRegion.End)
            {
                Debug.LogWarning(@$"There was a call to Play() when the timeline has reached its end.
                    Silly goose, there will be no music here, and that is expected.
                    Seek ye to an earlier point in the timeline before ye calleth Play().");
                return;
            }
            Debug.Log("Timeline is playing");
            _clock.IsPaused = false;
        }

        public void Pause()
        {
            Debug.Log("Timeline is paused");
            _clock.IsPaused = true;
        }

        protected void ThrowIfInactive()
        {
            if (!Active.Value)
                throw new Exception("You cannot perform this action while the timeline is inactive");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Pause();
        }
    }
}
