namespace UniBeat.RhythmEngine
{
    /// <summary>
    /// The scoring result of a <see cref="HitObjectEvent"/>.
    /// </summary>
    public class Judgement
    {
        /// <summary>
        /// Whether this <see cref="Judgement"/> is the result of a hit or a miss.
        /// </summary>
        public HitResult Type;

        /// <summary>
        /// The <see cref="HitObject"/> which was judged.
        /// </summary>
        public readonly HitObjectEvent HitObject;

        /// <summary>
        /// The <see cref="JudgementCalculator"/> which this <see cref="Judgement"/> uses to calculate results.
        /// </summary>
        public readonly JudgementCalculator JudgementCalc;

        /// <summary>
        /// The offset from a perfect hit at which this <see cref="Judgement"/> occurred.
        /// Populated when this <see cref="Judgement"/> is applied via <see cref="DrawableHitObject.ApplyResult"/>.
        /// </summary>
        public double TimeOffset { get; internal set; }

        /// <summary>
        /// The absolute time at which this <see cref="Judgement"/> occurred.
        /// Equal to the (end) time of the <see cref="HitObject"/> + <see cref="TimeOffset"/>.
        /// </summary>
        public double TimeAbsolute => HitObject.StartTime.TotalMilliseconds + TimeOffset;

        /// <summary>
        /// The health prior to this <see cref="Judgement"/> occurring.
        /// </summary>
        public double HealthAtJudgement { get; internal set; }

        /// <summary>
        /// The combo prior to this <see cref="Judgement"/> occurring.
        /// </summary>
        public int ComboAtJudgement { get; internal set; }

        /// <summary>
        /// The highest combo achieved prior to this <see cref="Judgement"/> occurring.
        /// </summary>
        public int HighestComboAtJudgement { get; internal set; }

        /// <summary>
        /// Whether the user was in a failed state prior to this <see cref="Judgement"/> occurring.
        /// </summary>
        public bool FailedAtJudgement { get; internal set; }

        /// <summary>
        /// Whether a miss or hit occurred.
        /// </summary>
        public bool HasResult => Type > HitResult.None;

        /// <summary>
        /// Whether a successful hit occurred.
        /// </summary>
        public bool IsHit => Type.IsHit();

        public OffsetType OffsetType => TimeOffset > 0d ? OffsetType.Late : OffsetType.Early;
        public bool IsLate => OffsetType == OffsetType.Late;
        public bool IsEarly => OffsetType == OffsetType.Early;
        public bool IsSlightlyEarly => IsEarly && Type == HitResult.Good || Type == HitResult.Ok;
        public bool IsVeryEarly => IsEarly && Type == HitResult.Miss || Type == HitResult.Meh;
        public bool IsSlightlyLate => IsLate && Type == HitResult.Good || Type == HitResult.Ok;
        public bool IsVeryLate => IsLate && Type == HitResult.Miss || Type == HitResult.Meh;
        public bool IsMistake => Type <= HitResult.Meh;
        public bool IsPass => Type >= HitResult.Ok;

        /// <summary>
        /// Creates a new <see cref="Judgement"/>.
        /// </summary>
        /// <param name="hitObject">The <see cref="HitObject"/> which was judged.</param>
        /// <param name="judgement">The <see cref="JudgementCalc"/> to refer to for scoring information.</param>
        public Judgement(HitObjectEvent hitObject, JudgementCalculator judgementCalc)
        {
            HitObject = hitObject;
            JudgementCalc = judgementCalc;
        }

        public override string ToString() => $"{Type} (Score:{JudgementCalc.NumericResultFor(this)} HP:{JudgementCalc.HealthIncreaseFor(this)} {JudgementCalc})";
    }
}
