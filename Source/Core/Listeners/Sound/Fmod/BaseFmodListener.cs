using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace UniBeat.RhythmEngine
{
    public abstract class BaseFmodListener<T> : BaseListener<T>
        where T : ITimelineEvent
    {
        protected GameObject _fmodContainer;
        protected readonly Dictionary<string, FmodStudioEventSynchroniser> _fmodEventNameMap = new Dictionary<string, FmodStudioEventSynchroniser>();

        public BaseFmodListener(ITimelineTimeProvider timeline) : base(timeline) { }

        // Ensure that any FMOD events are emitted from a game object that
        // is following the timeline's index (via the TimelineBeatFollower).
        // This ensure the 3D Audio effect is accurate.
        protected FmodStudioEventSynchroniser CreateSynchoniser(string fmodEventPath)
        {
            if (_fmodContainer == null)
                _fmodContainer = CreateFmodContainer();

            if (_fmodEventNameMap.ContainsKey(fmodEventPath))
                return _fmodEventNameMap[fmodEventPath];

            // Event must reference an existing fmod event path
            var fmodEvent = _fmodContainer.AddComponent(typeof(StudioEventEmitter)) as StudioEventEmitter;
            var fmodSync = _fmodContainer.AddComponent(typeof(FmodStudioEventSynchroniser)) as FmodStudioEventSynchroniser;

            fmodEvent.EventReference = FMODUnity.RuntimeManager.PathToEventReference(fmodEventPath);
            fmodEvent.Preload = true;

            fmodSync.SetStudioEventEmitter(fmodEvent);
            _fmodEventNameMap.Add(fmodEventPath, fmodSync);
            return fmodSync;
        }

        protected virtual GameObject CreateFmodContainer()
        {
            var obj = new GameObject();
            obj.name = $"{this.GetType().Name} :: FMOD Events Container";
            obj.transform.parent = _timeline.transform;
            if (_timeline is ITimelineDrawable)
                obj.AddComponent<TimelineBeatFollower>();
            return obj;
        }
    }
}
