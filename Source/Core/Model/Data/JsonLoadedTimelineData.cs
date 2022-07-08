using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Cysharp.Threading.Tasks;

namespace UniBeat.RhythmEngine
{
    public class JsonLoadedTimelineData : TimelineData
    {
        public static async UniTask<JsonLoadedTimelineData> Load(string json)
        {
            if (json == null)
                return null;

            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                TraceWriter = new Tracer(),
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var data = JsonConvert.DeserializeObject<JsonLoadedTimelineData>(json, settings);

            UnityEngine.Debug.Log($"After initial deserialization we have a total of {data.Events?.Count} events in the Timeline.");

            await data.Init();

            UnityEngine.Debug.Log($"After running event generators, we now have a total of {data.Events?.Count} events in the Timeline.");

            return data;
        }

        [OnDeserialized]
        internal void OnSerializedMethod(StreamingContext context)
        {
            // Insert the default TempoEvent if there is none at index 0
            if (Events.OfType<TempoEvent>().Where(e => e.Index == 0 || e.Offset == TimeSpan.Zero).Count() == 0)
                Events.Add(TempoEvent.Default);
        }
    }

    class Tracer : ITraceWriter
    {
        public TraceLevel LevelFilter { get; set; } = TraceLevel.Warning;
        public void Trace(
            TraceLevel level,
            string message,
            Exception ex)
        {
            UnityEngine.Debug.Log($"{message} || {ex?.Message}");
        }
    }
}
