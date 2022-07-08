using System;
using System.Collections.Generic;
using UniBeat.Lib;

namespace UniBeat.RhythmEngine
{
    [Serializable]
    public class ScoreResult
    {
        public ScoreGrade Grade { get; set; }
        public long TotalScore { get; set; }
        public double Accuracy { get; set; }
        public string DisplayAccuracy => Accuracy.FormatAccuracy();
        public int Combo { get; set; } // the combo they ended the level on
        public int HighestCombo { get; set; } // the highest combo they achieved during the level
        public bool IsFullCombo { get; set; } // did they hit every object in the timeline? (combo = highestcombo = maxcombo)
        public DateTimeOffset Date { get; set; }
        public Dictionary<HitResult, int> Statistics = new Dictionary<HitResult, int>();
        public int Hits { get; set; }
        public int Lates { get; set; }
        public int Earlies { get; set; }
        public double AverageOffset { get; set; }
        public double MedianOffset { get; set; }
        public double StdDeviationOffset { get; set; }
        public int Misses => Statistics[HitResult.Miss];
        public int Mehs => Statistics[HitResult.Meh];
        public int Oks => Statistics[HitResult.Ok];
        public int Goods => Statistics[HitResult.Good];
        public int Greats => Statistics[HitResult.Great];
        public int Perfects => Statistics[HitResult.Perfect];
        public bool Passed => Grade.Normalised >= 3;
        public bool Perfect => Grade.Normalised >= 5;

        public override string ToString()
        {
            return @$"Grade: {Grade} | TotalScore: {TotalScore} | Accuracy: {DisplayAccuracy} |
            Combo: {Combo} | HighestCombo: {HighestCombo} | IsFullCombo: {IsFullCombo} |
            Perfects: {Perfects} | Greats: {Greats} | Goods: {Goods} | Oks: {Oks} | Mehs: {Mehs} | Misses: {Misses} |
            Hits: {Hits} | Earlies: {Earlies} | Lates: {Lates} |
            Average Offset: {AverageOffset.ToString("F2")} | Median Offset: {MedianOffset.ToString("F2")} | Std Deviation: {StdDeviationOffset.ToString("F2")} |";
        }
    }
}

