using System;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UniBeat.Lib;
namespace UniBeat.RhythmEngine
{
    // Base interface from which all other timeline flavours extend.
    public interface ITimeline
    {
        IObservable<TimelineData> Init { get; }
        ReactiveProperty<bool> Active { get; }
        ReactiveProperty<bool> DebugMode { get; }
        bool IsComplete { get; }
        IObservable<ITimelineEvent> GameplayRuntimeEvents { get; }
        IObservable<ProgressEvent> Progress { get; }
        IObservable<T> GameplayRuntimeEventsOfType<T>() where T : ITimelineEvent;
        TimelineRange PlaybackRegion { get; }
        bool DataIsLoaded { get; }
        TimelineData Data { get; }
        InputStyle InputStyle { get; }
        IObservable<ButtonWasPressed> ButtonWasPressed { get; }
        IObservable<ButtonWasReleased> ButtonWasReleased { get; }
        TimeSpan AudioLatency { get; } // How long it takes for the user to hear a sound after we call play()
        TimeSpan VisualLatency { get; } // How long it takes for the screen to display visuals after a GPU render call.
        TimeSpan InputLatency { get; } // How long it takes for Unity to register input after the player performs the input.
        Transform transform { get; }
        UniTaskVoid SetConfig(TimelineConfig config);
    }
}
