namespace UniBeat.RhythmEngine
{
    public enum TimelineDirection
    {
        LeftToRight = 1,
        RightToLeft = -1
    }

    public static class TimelineDirectionExtensions
    {
        public static bool IsLeftToRight(this TimelineDirection dir)
        {
            return dir == TimelineDirection.LeftToRight;
        }
    }
}
