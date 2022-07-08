using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UniBeat.RhythmEngine
{
    [Serializable]
    public class Score
    {
        public ScoreResult ScoreResult = new ScoreResult();
        public Replay Replay = new Replay();

        // [JsonConverter(typeof(StringEnumConverter))]
        // public TimelineGameMode Mode;
        public string LevelName;
        public string AreaName;
    }
}
