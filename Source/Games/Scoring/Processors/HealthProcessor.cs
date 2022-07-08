using System;
using UniBeat.Lib;

namespace UniBeat.RhythmEngine
{
    public class HealthProcessor : JudgementProcessor
    {

        /// <summary>
        /// Invoked when the <see cref="ScoreProcessor"/> is in a failed state.
        /// Return true if the fail was permitted.
        /// </summary>
        public event Func<bool> Failed;

        /// <summary>
        /// Additional conditions on top of <see cref="DefaultFailCondition"/> that cause a failing state.
        /// Used by accessibility modes / mods to prevent failure entirely, or make failure instant if any health is lost, etc.
        /// </summary>
        public event Func<HealthProcessor, Judgement, bool> FailConditions;

        /// <summary>
        /// The current health.
        /// </summary>
        public double Health => _health;
        protected double _health = 1d;
        private const double HEALTH_MIN = 0d;

        /// <summary>
        /// Whether this ScoreProcessor has already triggered the failed state.
        /// </summary>
        public bool HasFailed { get; private set; }


        protected override void ApplyResultInternal(Judgement result)
        {
            result.HealthAtJudgement = Health;
            result.FailedAtJudgement = HasFailed;

            if (HasFailed)
                return;

            _health += GetHealthIncreaseFor(result);

            if (!DefaultFailCondition && FailConditions?.Invoke(this, result) != true)
                return;

            if (Failed?.Invoke() != false)
                HasFailed = true;
        }

        protected override void RevertResultInternal(Judgement result)
        {
            _health = result.HealthAtJudgement;
        }

        /// <summary>
        /// Retrieves the health increase for a <see cref="Judgement"/>.
        /// </summary>
        /// <param name="result">The <see cref="Judgement"/>.</param>
        /// <returns>The health increase.</returns>
        protected virtual double GetHealthIncreaseFor(Judgement result) => result.JudgementCalc.HealthIncreaseFor(result);


        /// <summary>
        /// The default conditions for failing.
        /// </summary>
        protected virtual bool DefaultFailCondition => Precision.AlmostBigger(HEALTH_MIN, Health);

        protected override void Reset()
        {
            base.Reset();

            _health = 1;
            HasFailed = false;
        }

    }
}
