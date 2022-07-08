using System.Diagnostics;

namespace UniBeat.RhythmEngine
{
    public enum HitResult
    {
        /// <summary>
        /// Indicates that the object has not been judged yet.
        /// </summary>
        None,

        /// <summary>
        /// Indicates that the object has been judged as a miss.
        /// </summary>
        /// <remarks>
        /// This miss window (see HitWindows) determines how early a hit can be before it is considered for judgement (as opposed to being ignored as
        /// "too far in the future/past"). It should also define when a forced miss should be triggered (as a result of no user input in time).
        /// </remarks>
        Miss,

        Meh,

        Ok,

        Good,

        Great,

        Perfect,

        /// <summary>
        /// Indicates a miss that should be ignored for scoring purposes.
        /// </summary>
        IgnoreMiss,

        /// <summary>
        /// Indicates a hit that should be ignored for scoring purposes.
        /// </summary>
        IgnoreHit
    }

    public static class HitResultExtensions
    {

        /// <summary>
        /// Whether a <see cref="HitResult"/> increases/decreases the combo, and affects the combo portion of the score.
        /// </summary>
        public static bool AffectsCombo(this HitResult result)
        {
            switch (result)
            {
                case HitResult.Miss:
                case HitResult.Meh:
                case HitResult.Ok:
                case HitResult.Good:
                case HitResult.Great:
                case HitResult.Perfect:
                    return true;

                default:
                    return false;
            }
        }


        /// <summary>
        /// Whether a <see cref="HitResult"/> affects the accuracy portion of the score.
        /// </summary>
        public static bool AffectsAccuracy(this HitResult result)
            => IsScorable(result);



        /// <summary>
        /// Whether a <see cref="HitResult"/> represents a successful hit.
        /// </summary>
        public static bool IsHit(this HitResult result)
        {
            switch (result)
            {
                case HitResult.None:
                case HitResult.IgnoreMiss:
                case HitResult.Miss:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Whether a <see cref="HitResult"/> is scorable.
        /// </summary>
        public static bool IsScorable(this HitResult result) => result >= HitResult.Miss && result < HitResult.IgnoreMiss;

        /// <summary>
        /// Whether a <see cref="HitResult"/> is valid within a given <see cref="HitResult"/> range.
        /// </summary>
        /// <param name="result">The <see cref="HitResult"/> to check.</param>
        /// <param name="minResult">The minimum <see cref="HitResult"/>.</param>
        /// <param name="maxResult">The maximum <see cref="HitResult"/>.</param>
        /// <returns>Whether <see cref="HitResult"/> falls between <paramref name="minResult"/> and <paramref name="maxResult"/>.</returns>
        public static bool IsValidHitResult(this HitResult result, HitResult minResult, HitResult maxResult)
        {
            if (result == HitResult.None)
                return false;

            if (result == minResult || result == maxResult)
                return true;

            Debug.Assert(minResult <= maxResult);
            return result > minResult && result < maxResult;
        }
    }
}
