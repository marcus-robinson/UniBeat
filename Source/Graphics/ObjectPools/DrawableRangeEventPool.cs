using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;
namespace UniBeat.RhythmEngine
{
    // Creates an instance of `T` when a new range of the timeline falls within the visible area and is bigger than config.MinimumRangeSize.
    public abstract class DrawableRangeEventPool<T, K> : BaseTimelinePool<T, K>
        where T : DrawableTimelineEvent<K>
        where K : RangeEvent
    {
        public DrawableRangeEventPool(ITimelineDrawable timeline, DrawableRangeEventPoolConfig config, Transform hierarchyParent) : base(timeline, config, hierarchyParent)
        {
            timeline.Init.Subscribe(_ =>
            {
                if (config.MinimumDrawableWidth >= timeline.VisibleAreaBleed * 0.8)
                {
                    Debug.LogWarning(@$"
                    Your MinimumDrawableWidth value of {config.MinimumDrawableWidth} is very close to
                    the timeline's VisibleAreaBleed of {timeline.VisibleAreaBleed}. This may result in
                    the user seeing chunks of empty space before this pool will build objects to fill
                    the new portion of the timeline, since the VisibleArea events are emitted infrequently.
                    TLDR; Increase the timeline's VisibleAreaBleed or decrease this pool's MinimumDrawableWidth
                    if you notice chunks of empty space.");
                }
            });
        }

        private int MinSize => ((DrawableRangeEventPoolConfig)_config).MinimumDrawableWidth;

        protected override async UniTask<bool> BuildObjectsIn(TimelineRange range)
        {
            if (
                // We only refuse to draw if this range is in the middle of the timeline.
                // If we're right up against the start / end, then we draw even if
                // the width of this range is smaller than we'd prefer, since it can't possibly
                // get any bigger than this.
                range.Start > _timeline.PlaybackRegion.Start &&
                range.End < _timeline.PlaybackRegion.End &&
                _timeline.WidthOfRange(range) < MinSize
            )
            {
                // Debug.Log($"@@ NOT BUILDING -- {range} is smaller than {MinSize}");
                return false;
            }

            var drawableTimelineObject = await RentAsync().ToUniTask();
            drawableTimelineObject.Draw(CreateEvent(range), _timeline);

            // Debug.Log($"%% SUCCESS BUILT -- {range}");
            return true;
        }

        protected abstract K CreateEvent(TimelineRange range);
    }
}


