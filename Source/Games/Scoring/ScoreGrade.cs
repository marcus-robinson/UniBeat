using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UniBeat.RhythmEngine
{
    public enum GradeLetter
    {
        F = 0,
        D = 1,
        C = 2,
        B = 3,
        A = 4,
        S = 5,
        X = 6
    }

    [Serializable]
    public class ScoreGrade
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public GradeLetter Letter;
        public bool Plus;
        public bool Minus;
        public int Normalised => (int)Letter;

        public override string ToString()
        {
            return $"{Letter}{ (Plus ? "+" : Minus ? "-" : "") }";
        }

        public static ScoreGrade Fminus => new ScoreGrade() { Letter = GradeLetter.F, Minus = true };
        public static ScoreGrade F => new ScoreGrade() { Letter = GradeLetter.F };
        public static ScoreGrade Fplus => new ScoreGrade() { Letter = GradeLetter.F, Plus = true };
        public static ScoreGrade Dminus => new ScoreGrade() { Letter = GradeLetter.D, Minus = true };
        public static ScoreGrade D => new ScoreGrade() { Letter = GradeLetter.D };
        public static ScoreGrade Dplus => new ScoreGrade() { Letter = GradeLetter.D, Plus = true };
        public static ScoreGrade Cminus => new ScoreGrade() { Letter = GradeLetter.C, Minus = true };
        public static ScoreGrade C => new ScoreGrade() { Letter = GradeLetter.C };
        public static ScoreGrade Cplus => new ScoreGrade() { Letter = GradeLetter.C, Plus = true };
        public static ScoreGrade Bminus => new ScoreGrade() { Letter = GradeLetter.B, Minus = true };
        public static ScoreGrade B => new ScoreGrade() { Letter = GradeLetter.B };
        public static ScoreGrade Bplus => new ScoreGrade() { Letter = GradeLetter.B, Plus = true };
        public static ScoreGrade Aminus => new ScoreGrade() { Letter = GradeLetter.A, Minus = true };
        public static ScoreGrade A => new ScoreGrade() { Letter = GradeLetter.A };
        public static ScoreGrade Aplus => new ScoreGrade() { Letter = GradeLetter.A, Plus = true };
        public static ScoreGrade Sminus => new ScoreGrade() { Letter = GradeLetter.S, Minus = true };
        public static ScoreGrade S => new ScoreGrade() { Letter = GradeLetter.S };
        public static ScoreGrade Splus => new ScoreGrade() { Letter = GradeLetter.S, Plus = true };
        public static ScoreGrade X => new ScoreGrade() { Letter = GradeLetter.X };
    }


}
