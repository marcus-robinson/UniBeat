using System;

namespace UniBeat.RhythmEngine
{

    public interface ITimelineTimeProvider : ITimeline
    {
        // Return the clock time for a given sixteenth index on the timeline
        TimeSpan TimeOf(double index);
        double IndexOf(TimeSpan time);
        IObservable<int> BeatIndex { get; }
        IObservable<int> SixteenthIndex { get; }
        IObservable<bool> Paused { get; }
        IObservable<double> OnSeek { get; }
        TimeSpan CurrentSixteenthDuration { get; }
        TimeSpan CurrentBeatDuration { get; }
        TimeSpan CurrentTime { get; }
        double CurrentIndex { get; }
        double CurrentBeat { get; }
        Tempo CurrentTempo { get; }
        int NextIndex { get; }
        int PreviousIndex { get; }
        bool IsPaused { get; }
        bool AtFinalIndex { get; }
    }
}
