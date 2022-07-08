namespace UniBeat.RhythmEngine
{
    public interface ITimelineEventParent : ITimelineEvent
    {
        TempoEvent Tempo { get; }
    }
}
