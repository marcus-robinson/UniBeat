using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using UniBeat.Lib;

namespace UniBeat.RhythmEngine
{
    public class ScoreProcessor : JudgementProcessor
    {
        // Regardless as to how long a timeline / game is, the max score is always the same.
        // A perfect run on a a timeline of 7 objects will net you the same score as a perfect
        // run on a giant 100 object timeline. MAX_SCORE is this perfect run value.
        private const double MAX_SCORE = 1000000d;
        private double _scoreMultiplier = 1; // Mods can change this to adjust scoring.
        private double _baseScore; // The current base score before combo/accuracy scalers are applied.
        private double _lateOffsetsSum;
        private double _earlyOffsetsSum;
        private readonly Dictionary<HitResult, int> _scoreResultCounts = new Dictionary<HitResult, int>();
        private readonly List<Judgement> _judegements = new List<Judgement>();

        /// <summary>
        /// The total number of early hits the player attempted.
        /// </summary>
        public int TotalEarlies { get; private set; }

        /// <summary>
        /// The total number of early hits the player attempted.
        /// </summary>
        public int TotalLates { get; private set; }

        /// <summary>
        /// The total number of valid hits the player attempted.
        /// </summary>
        public int TotalHits { get; private set; }

        /// <summary>
        /// The maximum achievable base score.
        /// </summary>
        public double MaxBaseScore { get; private set; }

        /// <summary>
        /// The maximum achievable combo.
        /// </summary>
        public int MaxAchievableCombo { get; private set; }

        public ScoreGrade Grade { get; private set; }

        /// <summary>
        /// The current total score.
        /// </summary>
        public double TotalScore { get; private set; }

        /// <summary>
        /// The current accuracy.
        /// </summary>
        public ReactiveProperty<double> Accuracy { get; private set; } = new ReactiveProperty<double>();

        /// <summary>
        /// The current combo.
        /// </summary>
        public ReactiveProperty<int> Combo { get; private set; } = new ReactiveProperty<int>();

        /// <summary>
        /// The highest combo achieved by this score.
        /// </summary>
        public int HighestCombo { get; private set; }

        /// <summary>
        /// The default portion of <see cref="MAX_SCORE"/> awarded for hitting <see cref="HitObject"/>s accurately. Defaults to 30%.
        /// </summary>
        protected virtual double DefaultAccuracyPortion => 0.3;

        /// <summary>
        /// The default portion of <see cref="MAX_SCORE"/> awarded for achieving a high combo. Default to 70%.
        /// </summary>
        protected virtual double DefaultComboPortion => 0.7;

        public ScoreProcessor()
        {
            if (!Precision.AlmostEquals(1.0, DefaultAccuracyPortion + DefaultComboPortion))
                throw new InvalidOperationException($"{nameof(DefaultAccuracyPortion)} + {nameof(DefaultComboPortion)} must equal 1.");

            Combo.Subscribe(value => HighestCombo = Math.Max(HighestCombo, value));
            Accuracy.Subscribe(value => Grade = GradeFrom(value));
        }


        protected sealed override void ApplyResultInternal(Judgement result)
        {
            result.ComboAtJudgement = Combo.Value;
            result.HighestComboAtJudgement = HighestCombo;

            if (result.FailedAtJudgement)
                return;

            if (!result.Type.IsScorable())
                return;

            if (result.Type.AffectsCombo())
            {
                switch (result.Type)
                {
                    case HitResult.Miss:
                        Combo.Value = 0;
                        break;

                    default:
                        Combo.Value++;
                        break;
                }
            }

            double scoreIncrease = result.Type.IsHit() ? result.JudgementCalc.NumericResultFor(result) : 0;
            _baseScore += scoreIncrease;

            if (_scoreResultCounts.ContainsKey(result.Type))
                _scoreResultCounts[result.Type]++;
            else
                _scoreResultCounts.Add(result.Type, 1);

            if (result.IsLate)
            {
                TotalLates++;
                _lateOffsetsSum += Math.Abs(result.TimeOffset);
            }
            else if (result.IsEarly)
            {
                TotalEarlies++;
                _earlyOffsetsSum += Math.Abs(result.TimeOffset);
            }

            _judegements.Add(result);

            TotalHits++;

            updateScore();
        }

        protected sealed override void RevertResultInternal(Judgement result)
        {
            Combo.Value = result.ComboAtJudgement;
            HighestCombo = result.HighestComboAtJudgement;

            if (result.FailedAtJudgement)
                return;

            if (!result.Type.IsScorable())
                return;

            double scoreIncrease = result.Type.IsHit() ? result.JudgementCalc.NumericResultFor(result) : 0;


            _baseScore -= scoreIncrease;

            _scoreResultCounts[result.Type] = _scoreResultCounts[result.Type] - 1;

            TotalHits--;

            _judegements.Remove(result);

            updateScore();
        }

        private void updateScore()
        {
            Accuracy.Value = CalculateAccuracyRatio(_baseScore);
            TotalScore = getScore();
        }

        /// <summary>
        /// Retrieve a score populated with data for the current play this processor is responsible for.
        /// </summary>
        public virtual void PopulateScore(ScoreResult score)
        {
            score.TotalScore = (long)Math.Round(getScore());
            score.Combo = Combo.Value;
            score.HighestCombo = HighestCombo;
            score.IsFullCombo = Combo.Value == MaxAchievableCombo;
            score.Accuracy = Accuracy.Value;
            score.Grade = Grade;
            score.Date = DateTimeOffset.Now;
            score.Hits = TotalHits;
            score.Lates = TotalLates;
            score.Earlies = TotalEarlies;

            var offsets = _judegements.Select(j => Math.Abs(j.TimeOffset)).ToArray();
            score.AverageOffset = Calc.Mean(offsets);
            score.MedianOffset = Calc.Median(offsets);
            score.StdDeviationOffset = Calc.StandardDeviation(offsets);

            foreach (var result in Enum.GetValues(typeof(HitResult)).OfType<HitResult>().Where(r => r.IsScorable()))
                score.Statistics[result] = GetStatistic(result);
        }

        public int GetStatistic(HitResult result)
        {
            int value = 0;
            _scoreResultCounts.TryGetValue(result, out value);
            return value;
        }

        protected override void CacheMaxValues()
        {
            base.CacheMaxValues();
            MaxAchievableCombo = HighestCombo;
            MaxBaseScore = _baseScore;
        }

        /// <summary>
        /// Resets this ScoreProcessor to a default state.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            _scoreResultCounts.Clear();
            _judegements.Clear();

            _baseScore = 0;
            TotalHits = 0;
            TotalLates = 0;
            TotalEarlies = 0;

            TotalScore = 0;
            Accuracy.Value = 1;
            Combo.Value = 0;
            Grade = ScoreGrade.F;
            HighestCombo = 0;
        }


        private double getScore()
        {
            return GetScore(
                CalculateAccuracyRatio(_baseScore),
                CalculateComboRatio(HighestCombo)
            );
        }

        /// <summary>
        /// Computes the total score.
        /// </summary>
        /// <param name="accuracyRatio">The accuracy percentage achieved by the player.</param>
        /// <param name="comboRatio">The proportion of <paramref name="maxCombo"/> achieved by the player.</param>
        /// <returns>The total score.</returns>
        public double GetScore(double accuracyRatio, double comboRatio)
        {
            double accuracyScore = DefaultAccuracyPortion * accuracyRatio;
            double comboScore = DefaultComboPortion * comboRatio;
            return (MAX_SCORE * (accuracyScore + comboScore)) * _scoreMultiplier;
        }

        // If MaxBaseScore is not > 0, it means we're simulating a run to calculate a perfect score, so we just return 1.
        private double CalculateAccuracyRatio(double baseScore) => MaxBaseScore > 0 ? baseScore / MaxBaseScore : 1;

        // If MaxAchievableCombo is not > 0, it means we're simulating a run to calculate a perfect score, so we just return 1.
        private double CalculateComboRatio(int maxCombo) => MaxAchievableCombo > 0 ? (double)maxCombo / MaxAchievableCombo : 1;

        private ScoreGrade GradeFrom(double acc)
        {
            if (acc == 1)
                return ScoreGrade.X;
            if (acc > 0.95)
                return ScoreGrade.S;
            if (acc > 0.9)
                return ScoreGrade.A;
            if (acc > 0.8)
                return ScoreGrade.B;
            if (acc > 0.7)
                return ScoreGrade.C;

            // if (Perfects >= hits.Count / 2)
            // {
            //     ToPlus();
            // }
            // else if (Mistakes >= hits.Count / 2)
            // {
            //     ToMinus();
            // }

            return ScoreGrade.D;
        }

    }
}
