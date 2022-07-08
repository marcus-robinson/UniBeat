using System.Linq;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;

namespace UniBeat.RhythmEngine
{
    public abstract class BaseTimelineEventGenerator : BaseTimelineEvent, ITimelineEventGenerator
    {
        public TempoEvent Tempo { get; private set; }
        public bool Completed { get; private set; }

        public async UniTask<IList<ITimelineEvent>> GenerateEvents(IList<TempoEvent> tempoEvents)
        {
            this.Tempo = tempoEvents
                .Where(e => e.Index <= Index)
                .OrderByDescending(e => e.Index)
                .First();

            var results = await InternalGetEvents();
            Completed = true;
            return results;
        }

        protected abstract UniTask<IList<ITimelineEvent>> InternalGetEvents();
    }
}
