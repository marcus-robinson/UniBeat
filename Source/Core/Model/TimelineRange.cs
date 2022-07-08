using System;
using UnityEngine;
using System.Collections.Generic;
using UniBeat.Lib;

namespace UniBeat.RhythmEngine
{
    // A set range on the timeline measured in sixteenths
    [Serializable]
    public class TimelineRange
    {
        public readonly double Start;// { get; private set; }
        public readonly double End;//  { get; private set; }
        public double Middle => (End - Start) / 2d;
        public double Size => End - Start;
        public int StartAsInt => (int)Math.Round(Start);
        public int EndAsInt => (int)Math.Round(End);
        public static TimelineRange Zero => new TimelineRange(0d, 0d);

        public TimelineRange(double start, double end)
        {
            if (start > end)
            {
                throw new Exception($"Invalid TimelineRange. Start value of {start} is bigger than the End value of {end}.");
            }
            if (start < 0)
            {
                throw new Exception($"Invalid TimelineRange. Start value of {start} cannot be less than zero.");
            }
            Start = start;
            End = end;
        }

        public TimelineRange Clone()
        {
            return new TimelineRange(Start, End);
        }

        // Increase the size of the range by adding additional area to the start.
        public TimelineRange ExpandStartBy(double add)
        {
            return StartAt(Mathf.Max(0, (float)(Start - add)));
        }

        // Increase the size of the range by adding additional area to the end.
        public TimelineRange ExpandEndBy(double add)
        {
            return EndAt(End + add);
        }

        // Increase the size of the range by adding additional area to the start and end.
        public TimelineRange ExpandStartAndEndBy(double add)
        {
            return ExpandStartBy(add).ExpandEndBy(add);
        }

        // Returns a new TimelineRange instance starting from a specified start index to the end of the current range.
        public TimelineRange StartAt(double index)
        {
            return new TimelineRange(index, End);
        }

        // Returns a new TimelineRange instance starting from a specified start index to the end of the current range.
        public TimelineRange EndAt(double index)
        {
            return new TimelineRange(Start, index);
        }

        // Does this range partially overlap another range by any amount.
        // Returns true if one starts exactly where the other ends.
        public bool Overlaps(TimelineRange other)
        {
            return Calc.RangesOverlap((float)Start, (float)End, (float)other.Start, (float)other.End) || other.End == Start || End == other.Start;
        }

        // Clamp a given index to this range.
        public double Clamp(double index)
        {
            return Mathf.Clamp((float)index, (float)Start, (float)End);
        }

        // Does this number fall within the range.
        public bool Contains(int index)
        {
            return Contains((double)index);
        }
        // Does this number fall within the range.
        public bool Contains(float index)
        {
            return Contains((double)index);
        }
        // Does this number fall within the range.
        public bool Contains(double index)
        {
            return Contains(new TimelineRange(index, index));
        }
        // Does this range *completely* subsume another range.
        public bool Contains(TimelineRange other)
        {
            return Calc.RangeContains((float)Start, (float)End, (float)other.Start, (float)other.End);
        }

        // SET OPERATIONS ////////////////////

        // Returns a new TimelineRange instance containing the range that are in this or the other and everything in between.
        public TimelineRange Union(TimelineRange other)
        {
            return new TimelineRange(Mathf.Min((float)Start, (float)other.Start), Mathf.Max((float)End, (float)other.End));
        }

        // Returns a new TimelineRange instance containing the range that are both in this and the other range.
        public TimelineRange Intersection(TimelineRange other)
        {
            if (!Overlaps(other))
                return TimelineRange.Zero;

            return new TimelineRange(Mathf.Max((float)Start, (float)other.Start), Mathf.Min((float)End, (float)other.End));
        }

        // Returns a list of TimelineRange instances containing the range that are in the other set but are not in this set.
        public IList<TimelineRange> Complement(TimelineRange other)
        {
            var complement = new List<TimelineRange>();
            if (Overlaps(other))
            {
                if (other.Start < Start)
                {
                    complement.Add(new TimelineRange(other.Start, Start));
                }

                if (other.End > End)
                {
                    complement.Add(new TimelineRange(End, other.End));
                }
            }
            else
            {
                // We don't overlap, so everything in the other set is not in this set and is a complement.
                complement.Add(new TimelineRange(other.Start, other.End));
            }
            return complement;
        }

        public override string ToString() => $"TimelineRange || Start: {Start.ToString("F2")} | End: {End.ToString("F2")} | Size: {Size.ToString("F2")}";

        public static bool operator ==(TimelineRange t1, TimelineRange t2)
        {
            return ((System.Object)t1) == null ? ((System.Object)t2) == null : t1.Equals(t2);
        }

        public static bool operator !=(TimelineRange t1, TimelineRange t2)
        {
            return ((System.Object)t1) == null ? ((System.Object)t2) != null : !t1.Equals(t2);
        }

        public override bool Equals(System.Object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var comparable = (TimelineRange)obj;

            return this.Start == comparable.Start && this.End == comparable.End;
        }

        public override int GetHashCode()
        {
            return (Start.GetHashCode() << 2) ^ End.GetHashCode();
        }
    }
}
