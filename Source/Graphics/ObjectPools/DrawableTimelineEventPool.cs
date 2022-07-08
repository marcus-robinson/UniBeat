using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
namespace UniBeat.RhythmEngine
{
    // Creates an instance of `T` whenever an event of type `K` appears on the timeline.
    public class DrawableTimelineEventPool<T, K> : BaseTimelinePool<T, K>
        where T : DrawableTimelineEvent<K>
        where K : ITimelineEvent
    {
        public DrawableTimelineEventPool(ITimelineDrawable timeline, ITimelinePoolConfig config, Transform hierarchyParent) : base(timeline, config, hierarchyParent) { }

        protected override async UniTask<bool> BuildObjectsIn(TimelineRange range)
        {
            var events = _data.InRange<K>(range);

            foreach (var e in events)
            {
                // Need this extra check, since _data.InRange<K> isn't strict enough.
                // Added this in because TaikoBeatEvent was incorrectly being rendered in the TaikoHitObjectEvent's pool.
                // K should be the exact type we want, not a parent event class, like BaseTimelineEvent
                if (e.GetType() == typeof(K) && !e.ExcludeFromTimeline)
                {
                    var drawableTimelineObject = await RentAsync().ToUniTask();
                    drawableTimelineObject.Draw(e, _timeline);
                }
            }

            return true;
        }
    }
}
