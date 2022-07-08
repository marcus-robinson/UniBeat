namespace UniBeat.RhythmEngine
{
    public interface ITimelinePlaybackControl : ITimelineTimeProvider
    {
        void SeekTo(double newIndex);
        void Play();
        void Pause();
    }
}
