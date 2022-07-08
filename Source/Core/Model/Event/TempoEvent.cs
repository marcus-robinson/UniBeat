using System;
using System.Runtime.Serialization;
using UnityEngine;
using Newtonsoft.Json;

namespace UniBeat.RhythmEngine
{
    [Serializable]
    // Define a new tempo for the timeline.
    public class TempoEvent : BaseTimelineEvent
    {
        // See the TempoEvent section of the README for an explanation of why
        // we have such a weird use of Indexes here.
        public double IndexRelativeToPriorBPM { get; set; }

        public override double Index
        {
            get
            {
                return Math.Ceiling(IndexRelativeToPriorBPM);
            }
            set
            {
                IndexRelativeToPriorBPM = value;
            }
        }

        public override TimelineRange Range => new TimelineRange(Index, EndIndex ?? Index);

        // IndexRelativeToPriorBPM must be set when a timing event is created, but Offset may NOT be set.
        // If Offset is left as Zero, then it will be populated in the below Init method based on the IndexRelativeToPriorBPM.
        public TimeSpan Offset = TimeSpan.Zero; // the offset from the start of the song where this timing event starts

        //////////////////////////////
        // EndIndex and EndOffset are derived fields that are populated during the cache build in TimelineData.
        // They define when this timing event's influence ends.
        // EndIndex/EndOffset will always correspond with the Index/Offset values of the next TempoEvent,
        // unless they are null, which means it's the last or only one in the timeline.
        [JsonIgnore]
        internal double? EndIndex = null;
        internal int EndIndexAsInt => (int)Math.Round(EndIndex ?? Index);
        //////////////////////////////

        public bool IsFinal => EndIndex == null;

        [JsonIgnore]
        public Tempo Tempo;

        public TimeSignature TimeSignature = TimeSignature.CommonTime;

        // The bounds this timing event occupies in unity space, relative to the parent timeline.
        public Bounds Bounds;

        // Convenience property for representing tempo in JSON, since the Tempo object is a pain.
        // Set via JSON deserialization only - not for public access.
        private double _internalBpmFromJson;
        public double BPM { get { return Tempo.BeatsPerMinute; } set { _internalBpmFromJson = value; } }

        [OnDeserialized]
        internal void OnSerializedMethod(StreamingContext context)
        {
            Tempo = new Tempo(RhythmicDuration.Quarter, _internalBpmFromJson);
        }

        // start and end are times are absolute values in ms
        public double BeatsInRange(int startTime, int endTime)
        {
            if (startTime < Offset.TotalMilliseconds)
            {
                throw new Exception("Range must be within this Timing Point.");
            }

            return (endTime - Math.Max(0, startTime)) / Tempo.BeatTimeSpan.TotalMilliseconds;
        }
        // start and end are time values in ms
        public double SixteenthsCountInRange(int startTime, int endTime)
        {
            return BeatsInRange(startTime, endTime) * Utils.SixteenthsPerBeat;
        }
        public double SixteenthsCountInRange(TimeSpan startTime, TimeSpan endTime)
        {
            return SixteenthsCountInRange((int)startTime.TotalMilliseconds, (int)endTime.TotalMilliseconds);
        }

        // A relative zero-index'd count of indexes that occur between the start of this event and a given absolute offset.
        public double SixteenthsCountAt(int offsetMs)
        {
            return BeatsInRange((int)Offset.TotalMilliseconds, offsetMs) * Utils.SixteenthsPerBeat;
        }

        // A relative zero-index'd count of indexes that occur between the start of this event and a given absolute offset.
        public double SixteenthsCountAt(TimeSpan offset)
        {
            return BeatsInRange((int)Offset.TotalMilliseconds, (int)offset.TotalMilliseconds) * Utils.SixteenthsPerBeat;
        }
        public double IndexOf(TimeSpan offset)
        {
            return Index + SixteenthsCountAt(offset);
        }

        // Return the absolute time of an index that falls within this Tempo Event's range.
        public TimeSpan TimeOf(double index)
        {
            // if ((EndIndex != null && index > EndIndex) || index < Index)
            //     Debug.LogWarning($"Are you sure you have the right tempo? The index {index} is outside of this tempo event's range of {Index} - {EndIndex}");

            var sixteenthDuration = Tempo.BeatTimeSpan.TotalMilliseconds / Utils.SixteenthsPerBeat;
            var relativeIndex = index - Index;

            // Convert this to ms
            var relativeTime = relativeIndex * sixteenthDuration;

            return Offset + TimeSpan.FromMilliseconds(relativeTime);
        }

        public double WidthOfBeat(float speed)
        {
            return WidthAtRelativeIndex(speed, Utils.SixteenthsPerBeat);
        }

        public double WidthOfIndex(float speed)
        {
            return WidthAtRelativeIndex(speed, 1);
        }

        // What is the width, given an index count relative to our Index value.
        public double WidthAtRelativeIndex(float speed, double relativeIndex)
        {
            relativeIndex = Math.Max(relativeIndex, 0d); // don't allow negative values
            var sixteenthTimeSpan = Tempo.BeatTimeSpan.TotalSeconds / Utils.SixteenthsPerBeat;
            return speed * sixteenthTimeSpan * relativeIndex;
        }

        // What is the width, given an absolute index value
        public double WidthAtAbsoluteIndex(float speed, double absoluteIndex)
        {
            if (absoluteIndex < Index)
                throw new Exception($"absoluteIndex value of {absoluteIndex} is less than the NewIndex value of {Index}");

            return WidthAtRelativeIndex(speed, absoluteIndex - Index);
        }

        // Every timeline MUST have a music timing event with index = 0 and offset = 0.
        // See README.md for details.
        public static TempoEvent Default = new TempoEvent()
        {
            IndexRelativeToPriorBPM = 0d,
            Offset = TimeSpan.Zero,
            Tempo = new Tempo(RhythmicDuration.Quarter, 1000)
        };

        public override bool Equals(System.Object obj)
        {
            var comparable = obj as TempoEvent;

            if (comparable == null)
            {
                // If it is null then it is not equal to this instance.
                return false;
            }

            return this.Tempo == comparable.Tempo && this.Index == comparable.Index;
        }

        public override int GetHashCode()
        {
            return (Tempo.GetHashCode() << 2) ^ Index.GetHashCode();
        }

    }
}
