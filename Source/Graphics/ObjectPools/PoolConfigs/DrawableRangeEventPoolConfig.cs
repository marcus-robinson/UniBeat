namespace UniBeat.RhythmEngine
{
    public class DrawableRangeEventPoolConfig : TimelinePoolConfig
    {
        // The smallest Unity-space width of a new chunk of the Timeline that will be drawn by the pool.
        public int MinimumDrawableWidth { get; set; } = 5;
    }
}
