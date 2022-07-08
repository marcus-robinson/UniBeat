using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace UniBeat.RhythmEngine
{
    [Serializable]
    public class TimelineData
    {
        public GameDifficulty BaseDifficulty;

        // A list of timeline events, always ordered by position.
        // This must be public so that JSON.net can write to it when deserializing level file.
        public List<ITimelineEvent> Events { get; set; } = new List<ITimelineEvent>(){
            TempoEvent.Default
        };

        private readonly Dictionary<int, List<ITimelineEvent>> _eventsByIndex = new Dictionary<int, List<ITimelineEvent>>();
        private readonly Dictionary<int, TempoEvent> _tempoByIndex = new Dictionary<int, TempoEvent>();
        // The range that we intend to play back
        public TimelineRange Size { get; private set; }

        public string Id;

        // Call this after the raw event data has been added to the Events List
        public async UniTask Init()
        {
            ProcessTempos();

            await RunEventGenerators();

            Validate();

            if (string.IsNullOrEmpty(Id))
            {
                Id = $"json_loaded_timeline";
            }

            PopulateMetadata();

            BuildEventCache();
        }

        public void Reset()
        {
            Events.ForEach(e => e.Reset());
        }

        public List<ITimelineEvent> EventsAtIndex(int index)
        {
            List<ITimelineEvent> events;
            _eventsByIndex.TryGetValue(index, out events);
            return events ?? new List<ITimelineEvent>();
        }

        public TempoEvent TempoAt(int index)
        {
            return _tempoByIndex[index];
        }

        // Return a new TimelineEvents instance containing only those events in the range provided.
        public List<ITimelineEvent> InRange(TimelineRange range)
        {
            return InRange<ITimelineEvent>(range);
        }

        // Return a new TimelineEvents instance containing only those events of type `T` in the range provided.
        public List<T> InRange<T>(TimelineRange range)
            where T : ITimelineEvent
        {
            Predicate<T> positionPredicate = e => e.Index >= range.Start && e.Index <= range.End;
            return Events.OfType<T>()?.ToList().FindAll(positionPredicate);
        }

        // Look backwards through the timeline events from `index` and find the first event of type `T`.
        public T MostRecent<T>(double index)
            where T : ITimelineEvent
        {
            var eventsWithSameOrSmallerIndex = Events.FindAll(tEvent => tEvent.Index <= index).OfType<T>();
            return eventsWithSameOrSmallerIndex.OrderByDescending(e => e.Index).FirstOrDefault();
        }
        public T First<T>()
            where T : ITimelineEvent
        {
            return Events.OfType<T>().FirstOrDefault();
        }

        protected async UniTask RunEventGenerators()
        {
            // Get a collection of async event generation tasks
            Func<IEnumerable<ITimelineEventGenerator>> getIncompleteGenerators = () => Events?.OfType<ITimelineEventGenerator>().Where(e => !e.Completed).ToList();

            var incomplete = getIncompleteGenerators();

            while (incomplete.Count() > 0)
            {
                var gen = incomplete.First();
                var tempoEvents = Events.OfType<TempoEvent>().ToList();
                Events.AddRange(await gen.GenerateEvents(tempoEvents));
                ProcessTempos();
                incomplete = getIncompleteGenerators();
            }

            // We used to run all these event generation tasks in parallel, but this meant that we could have no dependencies.
            // I switched to sequential operation so that we can have event generators that generate other event generators.
            // A new 'Complete' flag lets us know which ones to skip.
            //////////////////////////////////////////////
            // This is the old parallel execution code
            //var eventGenTasks = Events?.OfType<ITimelineEventGenerator>()?.Select(generator => generator.GenerateEvents(tempoEvents));
            // if (eventGenTasks != null)
            // {
            //     // Wait for the event generation to finish
            //     var generatedEvents = await UniTask.WhenAll(eventGenTasks);

            //     // Add the newly generated events to the timeline
            //     generatedEvents.ToList().ForEach(events => Events.AddRange(events));
            // }
        }


        // Quantize the events with their messy floating point indexes into a clean integer keyed dictionary.
        protected void BuildEventCache()
        {
            _eventsByIndex.Clear();
            _tempoByIndex.Clear();

            var eventstoExamine = InRange(new TimelineRange(1970, 1970));

            TempoEvent tempoAtThisIndex = MostRecent<TempoEvent>(Size.Start);
            var counter = 0;
            for (var i = (int)Size.Start; i <= Size.End; i++)
            {
                var events = InRange(new TimelineRange(Math.Max(0, i - 0.49999d), i + 0.5d));

                if (events.Count > 0)
                {
                    _eventsByIndex.Add(i, events);

                    var tempos = events.OfType<TempoEvent>();
                    if (tempos.Count() > 0)
                    {
                        tempoAtThisIndex = tempos.First();

                        if (tempos.Count() > 1)
                            UnityEngine.Debug.LogError($"Multiple tempo events found at index {i}");
                    }

                    events.OfType<HitObjectEvent>().ToList().ForEach(e =>
                    {
                        e.StartTime = tempoAtThisIndex.TimeOf(e.IndexAsInt);
                    });
                }

                // UnityEngine.Debug.Log($"{i} -- Music Event: {timingAtThisIndex.NewIndex}  || actual index: {timingAtThisIndex.Index}");
                _tempoByIndex.Add(i, tempoAtThisIndex); //cache the BPM at every sixteenth index\

                counter = i;
            }
        }

        protected void ProcessTempos()
        {
            RemoveDefaultTempoIfNotRequired();

            var orderedTempos = Events.OfType<TempoEvent>().OrderBy(e => e.Offset.TotalMilliseconds);
            TempoEvent priorTempo = orderedTempos.First<TempoEvent>();

            orderedTempos.ToList().ForEach(newTiming =>
            {
                if (newTiming == priorTempo)
                    return;

                priorTempo.EndIndex = newTiming.IndexRelativeToPriorBPM;

                // It's rare, but during quantization, we may pick up multiple music timing events.
                // For example, imagine this (real world example) of two timing events at indexes:
                // 1903.99873257288 and 1904.49596774194
                // when i = 1904 the range we search through is: 1903.50001 >= 1904 <= 1904.5000000
                // which will pick up BOTH of these. This is why we have all this weird complexity below.
                // If we have an extremely short timing event, we remove it.
                // Why? because if it's really short, then it may end before it starts,
                // i.e Index is a Math.Ceil (round up) of the IndexRelativeToPriorBPM, and if the
                // EndIndex arrives before the Math.Ceil, we get funky behaviour, so we correct it here.
                if (priorTempo.Index > priorTempo.EndIndex)
                {
                    UnityEngine.Debug.LogWarning($"Removing {priorTempo.Tempo.BeatsPerMinute}BPM tempo event at index {priorTempo.IndexAsInt} because it's tiny.");
                    Events.Remove(priorTempo);
                    priorTempo = newTiming;
                    return;
                }

                newTiming.Offset = priorTempo.TimeOf(newTiming.IndexRelativeToPriorBPM);

                priorTempo = newTiming;
            });
        }

        // We MUST have a Tempo Event at 0. The TempoEvent.Default event is inserted to guarantee this.
        // However, if the data already contains a TempoEvent at zero, we can safely remove the default.
        protected void RemoveDefaultTempoIfNotRequired()
        {
            var nonDefaultMusicEventsAtIndexZero = InRange(new TimelineRange(0, 0.5d)).OfType<TempoEvent>()
                .Where(e => e != TempoEvent.Default);
            if (nonDefaultMusicEventsAtIndexZero.Count() >= 1)
            {
                Events.Remove(Events.Find(e => e == TempoEvent.Default));
            }
        }

        protected void Validate()
        {
            var isValid =
                Events?.Count > 0 &&
                // Need one music timing event at the 0th index with an offset of 0. See README.md
                First<TempoEvent>().Index == 0 &&
                First<TempoEvent>().Offset == TimeSpan.Zero &&
                !String.IsNullOrEmpty(Id);

            if (!isValid)
                throw new Exception("Empty events list or missing a music timing event at 0th index with 0ms offset.");
        }

        protected void PopulateMetadata()
        {
            if (Events == null || Events.Count == 0)
                return;

            Events = Events.OrderBy(e => e.Index).ToList();

            Size = new TimelineRange(0d, Math.Ceiling(Events.Last().Index));

            UnityEngine.Debug.Log($"## Timeline Size is {Size.Size} ##");
        }
    }
}
