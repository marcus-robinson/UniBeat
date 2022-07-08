using System;
using System.Linq;
using UniRx;
using Cysharp.Threading.Tasks;

namespace UniBeat.RhythmEngine
{
    public abstract class BaseListener<T> : IDisposable
        where T : ITimelineEvent
    {
        protected ITimelineTimeProvider _timeline;
        protected CompositeDisposable _subscriptions;
        protected IDisposable _activeSub;
        public IObservable<Unit> Ready => _ready.AsObservable();
        private Subject<Unit> _ready = new Subject<Unit>();

        public BaseListener(ITimelineTimeProvider timeline)
        {
            _timeline = timeline;

            _timeline.Init.Subscribe(async data =>
            {
                if (data.Events.OfType<T>().Count() == 0)
                {
                    UnityEngine.Debug.LogWarning($"Listener for {typeof(T)} is not being activated, since the timeline has no events of this type.");
                    return;
                }

                // The first item is always the default `false` value  that the _timeline is instantiated with
                // which we can safely ignore.
                _activeSub = _timeline.Active.Skip(1).Subscribe(OnActiveChange);

                await Init(data);

                _ready.OnNext(Unit.Default);
                _ready.OnCompleted();
            });
        }

        protected abstract UniTask Init(TimelineData data);
        protected abstract void OnEvent(T e);

        protected virtual void OnActiveChange(bool active)
        {
            if (active)
            {
                StartListening();
            }
            else
            {
                StopListening();
            }
        }
        protected virtual void StartListening()
        {
            _subscriptions = new CompositeDisposable();
            _timeline.GameplayRuntimeEventsOfType<T>().Subscribe(e => OnEvent(e)).AddTo(_subscriptions);
        }
        protected virtual void StopListening()
        {
            _subscriptions?.Dispose();
        }

        public virtual void Dispose()
        {
            _subscriptions?.Dispose();
            _activeSub?.Dispose();
        }
    }
}
