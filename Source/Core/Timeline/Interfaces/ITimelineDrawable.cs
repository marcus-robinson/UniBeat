using UnityEngine;
using System;

namespace UniBeat.RhythmEngine
{
    public interface ITimelineDrawable : ITimelineTimeProvider
    {

        // How fast the timeline will move across the screen. This, along with BPM, determines the width of a sixteenth note.
        float Speed { get; }
        float VisibleAreaBleed { get; }

        // Given a sixteenth index on the timeline, return the local x position
        // if the Direction is left-to-right the return value will be positive, otherwise negative.
        float PositionOf(int index);
        float PositionOf(double index);

        // Given a local x position on the timeline, return the sixteenth index
        // if the Direction is left-to-right the localPositionX param will be positive, otherwise negative.
        double IndexOf(float localPositionX);

        // Unity-space width of a given range of the timeline
        float WidthOfRange(TimelineRange range);

        TimelinePlayhead GetPlayhead();

        // The width of single index at this point in the timeline.
        float WidthOfASixteenthBeatAt(double index);
        TimelineDirection Direction { get; }
        IObservable<TimelineRange> VisibleArea { get; }
        BoxCollider2D Collider { get; }
    }
}
