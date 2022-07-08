using System;
using UniRx;

namespace UniBeat.RhythmEngine
{
    public abstract class HitObjectEvent : BaseTimelineEvent
    {
        internal TimeSpan StartTime;
        public double? EndIndex;
        public HitSoundType HitSound;

        // How many sixteenth beats does this hitobject take up on the timeline
        public override TimelineRange Range => new TimelineRange(Index, EndIndex ?? Index);

        public bool ExcludeFromScoreCalculation = false;

        public bool Judged => Result.Type != HitResult.None;

        public Judgement Result { get; private set; }
        public TimeSpan InputLatency { get; set; } = TimeSpan.Zero;
        public ReactiveProperty<GameDifficulty> Difficulty { get; private set; } = new ReactiveProperty<GameDifficulty>();
        public TimeSpan BiggestHitWindow => TimeSpan.FromMilliseconds(_hitWindows.WindowFor(Result.JudgementCalc.MinResult));
        public TimeSpan SmallestHitWindow => TimeSpan.FromMilliseconds(_hitWindows.WindowFor(Result.JudgementCalc.MaxResult));

        protected HitWindows _hitWindows;
        public IObservable<Judgement> Results => _results.AsObservable();
        private readonly Subject<Judgement> _results = new Subject<Judgement>();
        protected bool _validActionPressed = true;


        /// <summary>
        /// Creates the <see cref="Judgement"/> that represents the scoring result for this <see cref="DrawableHitObject"/>.
        /// </summary>
        /// <param name="judgement">The <see cref="Judgement"/> that provides the scoring information.</param>
        public virtual Judgement CreateJudgement() => new Judgement(this, CreateCalculator());
        protected virtual JudgementCalculator CreateCalculator() => new JudgementCalculator();
        protected abstract HitWindows CreateHitWindows();

        public HitObjectEvent()
        {
            Result ??= CreateJudgement()
                             ?? throw new InvalidOperationException($"{GetType()} must provide a {nameof(Judgement)} through {nameof(CreateJudgement)}.");

            // Skip the first, since the ReactiveProperty always emits an initial event for the default value.
            // we're interested only in explicitly set values.
            Difficulty.Skip(1).Subscribe(value =>
            {
                // Hit windows can only be created once we have a difficulty set.
                _hitWindows ??= CreateHitWindows()
                             ?? throw new InvalidOperationException($"{GetType()} must provide a {nameof(HitWindows)} through {nameof(CreateHitWindows)}.");
                _hitWindows.SetDifficulty(value.OverallDifficulty);
            });
        }

        /// <summary>
        /// Processes this <see cref="HitObjectEvent"/>, checking if a scoring result has occurred.
        /// </summary>
        /// <param name="userTriggered">Whether the user triggered this process.</param>
        /// <returns>Whether a scoring result has occurred from this <see cref="HitObjectEvent"/> or any nested <see cref="HitObjectEvent"/>.</returns>
        public bool UpdateResult(bool userTriggered, ITimelineTimeProvider time)
        {
            if (Judged)
                return false;

            var latencyMs = userTriggered ? InputLatency : TimeSpan.Zero;

            // UnityEngine.Debug.Log($"Index: {IndexAsInt} | Offset: {(time.CurrentTime - latencyMs - StartTime).TotalMilliseconds}ms |_time.CurrentMilliseconds {time.CurrentTime.TotalMilliseconds} || calibration : {latencyMs}ms || HitObject.StartTime: {StartTime.TotalMilliseconds}ms");

            TryToApplyResult(userTriggered, time.CurrentTime - latencyMs - StartTime, time.AtFinalIndex);

            return Judged;
        }

        public override void Reset()
        {
            base.Reset();
            Result = CreateJudgement();
            _results.OnNext(Result);
        }

        /// <summary>
        /// Checks if a scoring result has occurred for this <see cref="HitObjectEvent"/>.
        /// </summary>
        /// <remarks>
        /// If a scoring result has occurred, this method must invoke <see cref="ApplyResult"/> to update the result and notify Rx streams.
        /// </remarks>
        /// <param name="userTriggered">Whether the user triggered this check.</param>
        /// <param name="offsetMs">The offset from the start time of the <see cref="HitObject"/> at which this result attempt occurred in ms.
        /// A <paramref name="offsetMs"/> &gt; 0 implies that this check occurred after the end time of the <see cref="HitObject"/>. </param>
        protected void TryToApplyResult(bool userTriggered, TimeSpan offset, bool atFinalIndex)
        {
            var offsetMs = offset.TotalMilliseconds;

            // someone is asking for a result other than a player input event
            if (!userTriggered)
            {
                // UnityEngine.Debug.Log($":: AUTOHIT :: Attempting to apply result without user intput {offsetMs}ms past the correct time, which should be a little more than the longest hit window which is: {BiggestHitWindow.TotalMilliseconds} || atFinalIndex? {atFinalIndex} || CanBeHit? : {_hitWindows.CanBeHit(offsetMs)}");

                // If we're at the end of the timeline, then the timeoffset may actually be pretty small,
                // since this means that this hit object probably falls on the final index of the timeline and the 'current time'
                // value of the timeline can never be higher than the StartTime of this event.
                // Which means timeOffset will look like a good hit, but the only way we get in here is if we've waited for the full hit window,
                // in which case, we have to give a miss judgement with an artifical offset.
                if (atFinalIndex)
                    ApplyResult(HitResult.Miss, BiggestHitWindow.TotalMilliseconds);
                // if we're outside of the possible hit window
                else if (!_hitWindows.CanBeHit(offsetMs))
                    ApplyResult(HitResult.Miss, offsetMs); // then we give the minimum result
                return; // Otherwise we do nothing and give the user a chance to hit this note
            }

            var result = _hitWindows.ResultFor(offsetMs);
            if (result == HitResult.None) // We're too far out for a judgement
                return;

            // UnityEngine.Debug.Log($"Index: {IndexAsInt} | Our timeoffset is {offsetMs} and we have a result! : {result}");

            if (!_validActionPressed)
            {
                UnityEngine.Debug.Log($"Index: {IndexAsInt} | INVALID ACTION | Result will be min {Result.JudgementCalc.MinResult} - Our timeoffset is {offsetMs}");
                ApplyResult(Result.JudgementCalc.MinResult, offsetMs);
            }
            else
            {

                ApplyResult(result, offsetMs);
            }
        }

        /// <summary>
        /// Applies the <see cref="Result"/> of this <see cref="HitObjectEvent"/>, notifying responders such as
        /// the <see cref="ScoreProcessor"/> of the <see cref="Judgement"/>.
        /// </summary>
        /// <param name="application">The callback that applies changes to the <see cref="Judgement"/>.</param>
        private void ApplyResult(HitResult result, double offsetMs)
        {
            if (Result.HasResult)
                throw new InvalidOperationException("Cannot apply result on a hitobject that already has a result.");

            Result.Type = result;

            if (!Result.HasResult)
                throw new InvalidOperationException($"{GetType()} applied a {nameof(Judgement)} but did not update {nameof(Judgement.Type)}.");

            Result.TimeOffset = Math.Min(MaximumJudgementOffset, offsetMs);

            _results.OnNext(Result);
        }

        /// <summary>
        /// The maximum offset from the time of <see cref="HitObject"/> at which this <see cref="HitObjectEvent"/> can be judged.
        /// The time offset of <see cref="Result"/> will be clamped to this value during <see cref="ApplyResult"/>.
        /// <para>
        /// Defaults to the miss window of <see cref="HitObject"/>.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This does not affect the time offset provided to invocations of <see cref="TryToApplyResult"/>.
        /// </remarks>
        protected virtual double MaximumJudgementOffset => _hitWindows?.WindowFor(HitResult.Miss) ?? 0d;
    }

    public enum HitSoundType
    {
        None = 0,
        Normal = 1,
        Whistle = 2,
        Finish = 4,
        Clap = 8
    }
}
