using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace UniBeat.RhythmEngine
{
    // Some timeline events generate additional events.
    public interface ITimelineEventGenerator : ITimelineEventParent
    {
        UniTask<IList<ITimelineEvent>> GenerateEvents(IList<TempoEvent> tempoEvents);

        bool Completed { get; }
    }
}
