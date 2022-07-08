
namespace UniBeat.RhythmEngine
{
    /// <summary>
    /// A <see cref="HealthProcessor"/> that accumulates health and causes a fail if the final health
    /// is less than a value required to pass the Timeline.
    /// </summary>
    public class AccumulatingHealthProcessor : HealthProcessor
    {
        protected override bool DefaultFailCondition => JudgedHits == MaxHits && Health < _requiredHealth;

        private readonly double _requiredHealth;

        /// <summary>
        /// Creates a new <see cref="AccumulatingHealthProcessor"/>.
        /// </summary>
        /// <param name="requiredHealth">The minimum amount of health required to beatmap.</param>
        public AccumulatingHealthProcessor(double requiredHealth)
        {
            _requiredHealth = requiredHealth;
        }

        protected override void Reset()
        {
            base.Reset();

            _health = 0;
        }
    }
}
