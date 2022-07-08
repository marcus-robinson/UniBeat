using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace UniBeat.RhythmEngine
{
    public class TimelinePlayhead : MonoBehaviour
    {
        public ReactiveProperty<bool> Enabled { get; set; } = new ReactiveProperty<bool>(true);
        protected ITimelineDrawable _timeline;
        private TimelineBeatFollower _follow;

        private bool _shouldFollow => _timeline.IsPaused && Enabled.Value;

        private CompositeDisposable _subs;

        protected virtual void Start()
        {
            _timeline = GetComponentInParent<ITimelineDrawable>();
            _follow = GetComponent<TimelineBeatFollower>();
            _subs = new CompositeDisposable();

            if (_timeline == null || _follow == null)
                throw new Exception("Playhead must be the child of a timeline and have a TimelineBeatFollower sibling.");

            _timeline.Init.Subscribe(_ => AddSubscriptions());
        }

        private void AddSubscriptions()
        {
            if (_timeline == null || !_timeline.DataIsLoaded)
                return;

            _timeline.Paused.Merge(Enabled).Subscribe(_ => _follow.Enabled.Value = !_timeline.IsPaused && Enabled.Value).AddTo(_subs);
            _timeline.Paused.Merge(Enabled).Where(val => val).Subscribe(_ => this.UpdateAsObservable().TakeWhile(_ => _shouldFollow).Subscribe(_ => UpdateWhenNotFollowingBeat())).AddTo(_subs);
            _timeline.Active.Subscribe(_ => UpdateWhenNotFollowingBeat()).AddTo(_subs);
        }

        protected virtual void OnDisable()
        {
            _subs?.Dispose();
            Enabled.Value = false;
        }

        protected virtual void OnEnable()
        {
            Enabled.Value = true;
            AddSubscriptions();
        }

        // Called every Update while the playhead is not following the timeline beat.
        protected virtual void UpdateWhenNotFollowingBeat()
        {
            // handle in child class
        }
    }
}
