using System;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace UniBeat.RhythmEngine
{
    // Update the xPos of the  game object's transform to follow the timeline's beat
    public class TimelineBeatFollower : MonoBehaviour
    {
        [SerializeField]
        ITimelineDrawable _timeline;
        public ReactiveProperty<bool> Enabled { get; set; } = new ReactiveProperty<bool>(false);

        private void Start()
        {
            if (_timeline == null)
                _timeline = GetComponentInParent<ITimelineDrawable>();

            if (_timeline == null)
                throw new Exception("TimelineBeatFollower must be the child of a timeline");

            Enabled.Where(val => val).Subscribe(_ => this.UpdateAsObservable().TakeWhile(_ => Enabled.Value).Subscribe(_ => UpdatePosition()));

            _timeline.Init.Subscribe(_ => Enabled.Value = true);
        }

        private void UpdatePosition()
        {
            var globalX = _timeline.transform.TransformPoint(new Vector3(_timeline.PositionOf(_timeline.CurrentIndex), 0, 0));
            transform.position = new Vector3(globalX.x, transform.position.y, transform.position.z);
        }
    }
}
