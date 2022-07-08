using System;
using System.Linq;
using UniRx;

namespace UniBeat.RhythmEngine
{
    public abstract class JudgementProcessor
    {
        /// <summary>
        /// Invoked when a new judgement has occurred. This occurs after the judgement has been processed by this <see cref="JudgementProcessor"/>.
        /// </summary>
        public IObservable<Judgement> NewJudgements => _newJudgements.AsObservable();

        private readonly Subject<Judgement> _newJudgements = new Subject<Judgement>();


        /// <summary>
        /// The maximum number of hits that can be judged.
        /// </summary>
        protected int MaxHits { get; private set; }

        /// <summary>
        /// The total number of judged <see cref="HitObject"/>s at the current point in time.
        /// </summary>
        public int JudgedHits { get; private set; }

        protected ITimeline _timeline;

        private Judgement lastAppliedResult;

        private bool hasCompleted;

        /// <summary>
        /// Whether all <see cref="Judgement"/>s have been processed.
        /// </summary>
        public bool HasCompleted => hasCompleted;

        /// <summary>
        /// Applies the score change of a <see cref="JudgementResult"/> to this <see cref="ScoreProcessor"/>.
        /// </summary>
        /// <param name="result">The <see cref="JudgementResult"/> to apply.</param>
        public void ApplyResult(Judgement result)
        {
            JudgedHits++;
            lastAppliedResult = result;

            ApplyResultInternal(result);

            _newJudgements.OnNext(result);
        }

        /// <summary>
        /// Reverts the score change of a <see cref="JudgementResult"/> that was applied to this <see cref="ScoreProcessor"/>.
        /// </summary>
        /// <param name="result">The judgement scoring result.</param>
        public void RevertResult(Judgement result)
        {
            JudgedHits--;

            RevertResultInternal(result);
        }

        /// <summary>
        /// Applies a <see cref="IBeatmap"/> to this <see cref="ScoreProcessor"/>.
        /// </summary>
        /// <param name="beatmap">The <see cref="IBeatmap"/> to read properties from.</param>
        public void SetTimeline(ITimeline timeline)
        {
            _timeline = timeline;
            _timeline.Init.Subscribe(data =>
            {
                ProcessTimeline(data);
            });
        }

        protected virtual void ProcessTimeline(TimelineData data)
        {
            Reset();
            GenerateMaxValues(data);
            CacheMaxValues();
            Reset();
        }

        protected virtual void CacheMaxValues()
        {
            MaxHits = JudgedHits;
        }

        /// <summary>
        /// Simulates an autoplay of a <see cref="TimelineData"/> to determine maximum perfect scoring values.
        /// </summary>
        /// <param name="data">The <see cref="TimelineData"/> to simulate.</param>
        protected virtual void GenerateMaxValues(TimelineData data)
        {
            foreach (var obj in data.InRange(_timeline.PlaybackRegion).OfType<HitObjectEvent>().Where(e => !e.ExcludeFromScoreCalculation))
                simulateMaxScore(obj);

            void simulateMaxScore(HitObjectEvent obj)
            {
                var judgement = obj.CreateJudgement();
                judgement.Type = judgement.JudgementCalc.MaxResult;
                ApplyResult(judgement);
            }
        }

        /// <summary>
        /// Applies the score change of a <see cref="JudgementResult"/> to this <see cref="ScoreProcessor"/>.
        /// </summary>
        /// <remarks>
        /// Any changes applied via this method can be reverted via <see cref="RevertResultInternal"/>.
        /// </remarks>
        /// <param name="result">The <see cref="JudgementResult"/> to apply.</param>
        protected abstract void ApplyResultInternal(Judgement result);

        /// <summary>
        /// Reverts the score change of a <see cref="JudgementResult"/> that was applied to this <see cref="ScoreProcessor"/> via <see cref="ApplyResultInternal"/>.
        /// </summary>
        /// <param name="result">The judgement scoring result.</param>
        protected abstract void RevertResultInternal(Judgement result);

        /// <summary>
        /// Resets this <see cref="JudgementProcessor"/> to a default state.
        /// </summary>
        protected virtual void Reset()
        {
            JudgedHits = 0;
        }
    }
}
