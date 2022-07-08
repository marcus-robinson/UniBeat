using System;
namespace UniBeat.RhythmEngine
{
    [Serializable]
    public class GameDifficulty
    {
        /// <summary>
        /// The default value used for all difficulty settings
        /// </summary>
        public const float DEFAULT_DIFFICULTY = 5;

        // Min val: 0 - easiest setting
        // Max val: 10 - hardest setting
        // Defaul val: 5 - normal mode
        public float OverallDifficulty { get; set; } = DEFAULT_DIFFICULTY;

        /// <summary>
        /// HP drain rate (HP) is a beatmap difficulty setting that controls how much health is passively lost to health drain.
        /// It also affects how heavily a player's health is penalized for missing notes and how much health is gained back by accurately hitting hit objects.
        /// </summary>
        public float DrainRate { get; set; } = DEFAULT_DIFFICULTY;
        public float CircleSize { get; set; } = DEFAULT_DIFFICULTY;


        private float? approachRate;

        public float ApproachRate
        {
            get => approachRate ?? OverallDifficulty;
            set => approachRate = value;
        }

        /// <summary>
        /// Returns a shallow-clone of this <see cref="BeatmapDifficulty"/>.
        /// </summary>
        public GameDifficulty Clone() => (GameDifficulty)MemberwiseClone();

        /// <summary>
        /// Maps a difficulty value [0, 10] to a two-piece linear range of values.
        /// </summary>
        /// <param name="difficulty">The difficulty value to be mapped.</param>
        /// <param name="min">Minimum of the resulting range which will be achieved by a difficulty value of 0.</param>
        /// <param name="mid">Midpoint of the resulting range which will be achieved by a difficulty value of 5.</param>
        /// <param name="max">Maximum of the resulting range which will be achieved by a difficulty value of 10.</param>
        /// <returns>Value to which the difficulty value maps in the specified range.</returns>
        public static double DifficultyRange(double difficulty, double min, double mid, double max)
        {
            if (difficulty > 5)
                return mid + (max - mid) * (difficulty - 5) / 5;
            if (difficulty < 5)
                return mid - (mid - min) * (5 - difficulty) / 5;

            return mid;
        }

        /// <summary>
        /// Maps a difficulty value [0, 10] to a two-piece linear range of values.
        /// </summary>
        /// <param name="difficulty">The difficulty value to be mapped.</param>
        /// <param name="range">The values that define the two linear ranges.
        /// <list type="table">
        ///   <item>
        ///     <term>od0</term>
        ///     <description>Minimum of the resulting range which will be achieved by a difficulty value of 0.</description>
        ///   </item>
        ///   <item>
        ///     <term>od5</term>
        ///     <description>Midpoint of the resulting range which will be achieved by a difficulty value of 5.</description>
        ///   </item>
        ///   <item>
        ///     <term>od10</term>
        ///     <description>Maximum of the resulting range which will be achieved by a difficulty value of 10.</description>
        ///   </item>
        /// </list>
        /// </param>
        /// <returns>Value to which the difficulty value maps in the specified range.</returns>
        public static double DifficultyRange(double difficulty, (double od0, double od5, double od10) range)
            => DifficultyRange(difficulty, range.od0, range.od5, range.od10);
    }
}
