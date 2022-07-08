using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniBeat.RhythmEngine
{
    public class TimelinePoolConfig : ITimelinePoolConfig
    {
        public virtual Func<UniTask<GameObject>> GetPrefab { get; set; }
    }
}
