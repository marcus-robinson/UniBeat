using System.Collections.Generic;

namespace UniBeat.RhythmEngine
{
    // An event that represents a specific range on the timeline.
    // The index is derived from the Start property of the range it represents.
    // Contains an array of all events that occur within this particular range.
    // For this reason, RangeEvents aren't "naturally occuring" timeline events,
    // they are derived events that represent a slice of the timeline, useful when some code
    // needs a wholistic view of a specific chunk of the timeline.
    public abstract class RangeEvent : BaseTimelineEvent
    {
        public override TimelineRange Range => _range;
        private TimelineRange _range;

        // The events that fall within this range
        public List<ITimelineEvent> Events { get; set; } = new List<ITimelineEvent>();

        public RangeEvent(TimelineRange range)
        {
            _range = range;
            Index = range.Start;
        }
    }
}
