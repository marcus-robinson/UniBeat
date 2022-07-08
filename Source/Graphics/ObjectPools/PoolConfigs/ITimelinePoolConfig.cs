using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniBeat.RhythmEngine
{
    public interface ITimelinePoolConfig
    {
        // The prefab we instantiate when building a chunk of the timeline
        Func<UniTask<GameObject>> GetPrefab { get; set; }
    }
}
