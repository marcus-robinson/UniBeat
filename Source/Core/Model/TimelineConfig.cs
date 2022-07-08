using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Cysharp.Threading.Tasks;
using UniBeat.Lib;
namespace UniBeat.RhythmEngine
{
    [Serializable]
    public class TimelineConfig
    {
        public TimeSpan AudioLatency = TimeSpan.Zero; // How long it takes for the user to hear a sound after we call play()
        public TimeSpan VisualLatency = TimeSpan.Zero; // How long it takes for the screen to display visuals after a GPU render call.
        public TimeSpan InputLatency = TimeSpan.Zero; // How long it takes for Unity to register input after the player performs the input.
        public float Height = 20f;
        public float Speed = 10f;
        public int VisibleAreaSampleRate = 30; // Every 30 frames, send a visible area update

        // How much of the grid, outside of the observable area do we consider 'visible', to the left and right.
        // Measured in Unity world space units. Since Visible Area events are emitted infrequently, we always
        // want a bit of bleed, otherwise you may briefly see blank portions of the grid as they come into view.
        // This value will need to increase as the above Speed value increases.
        public int VisibleAreaBleed = 20;

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(TimelineDirection.LeftToRight)]
        public TimelineDirection Direction;

        // The range that we intend to play back
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public TimelineRange PlaybackRegion = null;

        // TMP params so we can controls playback region via Unity inspector
        public int PlaybackStart = 0;
        public int PlaybackEnd = 0;

        [JsonIgnore]
        public IInputManager InputManager;

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(InputStyle.TriggerHitOnButtonPressed)]
        public InputStyle InputStyle = InputStyle.TriggerHitOnButtonPressed;

        public Func<UniTask<TimelineData>> GetTimelineData;
    }
}
