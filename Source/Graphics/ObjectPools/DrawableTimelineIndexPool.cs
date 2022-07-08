using UnityEngine;
using Cysharp.Threading.Tasks;

namespace UniBeat.RhythmEngine
{
    // Creates an instance of `T` at every index of the timeline.
    public class DrawableTimelineIndexPool<T> : BaseTimelinePool<T, IndexEvent>
        where T : DrawableTimelineEvent<IndexEvent>
    {
        public DrawableTimelineIndexPool(ITimelineDrawable timeline, ITimelinePoolConfig config, Transform hierarchyParent) : base(timeline, config, hierarchyParent) { }

        protected override async UniTask<bool> BuildObjectsIn(TimelineRange range)
        {
            var i = (int)range.Start;
            while (i <= range.End)
            {
                var drawableTimelineObject = await RentAsync().ToUniTask();
                drawableTimelineObject.Draw(new IndexEvent() { Index = i }, _timeline);
                i++;
            }
            return true;
        }
    }
}
