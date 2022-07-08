using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UniRx;
using Cysharp.Threading.Tasks;
using UniRx.Toolkit;
using UnityEngine;

namespace UniBeat.RhythmEngine
{
    public abstract class BaseTimelinePool<T, K> : AsyncObjectPool<T>
        where T : DrawableTimelineEvent<K>
        where K : ITimelineEvent
    {
        public ReactiveProperty<bool> Enabled = new ReactiveProperty<bool>();
        public ReadOnlyCollection<T> ActiveObjects;
        private readonly List<T> _activeObjects = new List<T>();
        protected TimelineData _data;
        protected readonly ITimelinePoolConfig _config;
        protected readonly Transform _hierarchyParent;
        protected readonly ITimelineDrawable _timeline;
        protected IDisposable _visibleAreaSub;
        protected TimelineRange _currentlyDrawnArea = TimelineRange.Zero;
        protected CompositeDisposable _subscriptions = new CompositeDisposable();

        public BaseTimelinePool(ITimelineDrawable timeline, ITimelinePoolConfig config, Transform hierarchyParent)
        {
            _config = config;
            _hierarchyParent = hierarchyParent;
            _timeline = timeline;
            _timeline.Init.Subscribe(events => _data = events);

            ActiveObjects = new ReadOnlyCollection<T>(_activeObjects);

            Enabled.Subscribe(value =>
            {
                if (value)
                {
                    _visibleAreaSub = _timeline.VisibleArea.Subscribe(visibleRange => Build(visibleRange));
                }
                else
                {
                    _currentlyDrawnArea = TimelineRange.Zero;
                    new List<T>(_activeObjects).ForEach(obj => Return(obj));
                    _visibleAreaSub?.Dispose();
                    Clear();
                }
            }).AddTo(_subscriptions);

            Enabled.Value = true;
        }

        protected virtual async void Build(TimelineRange visibleRange)
        {
            // We only build stuff that's within our timeline's defined range.
            visibleRange = _timeline.PlaybackRegion.Intersection(visibleRange);

            // Shortcircuit if the visile range is zero,
            // or it's non-zero but subsumed by the existing drawn area.
            if (visibleRange.Size == 0 || _currentlyDrawnArea.Contains(visibleRange))
                return;

            // Build new grids for the part of this new range we haven't currently drawn.
            var rangesToBuild = _currentlyDrawnArea.Complement(visibleRange);
            var buildTasks = rangesToBuild.ToList().FindAll(range => range.Size > 0).Select(range => BuildObjectsIn(range));

            // Wait for the build tasks to finish
            var results = await UniTask.WhenAll(buildTasks);

            foreach (var item in results.Select((success, index) => new { index, success }))
            {
                // Update the currently drawn range with this new range,
                // but only if it was successfully built.
                if (item.success)
                {
                    var range = rangesToBuild[item.index];
                    if (_currentlyDrawnArea.Overlaps(range) || _currentlyDrawnArea == TimelineRange.Zero)
                    {
                        _currentlyDrawnArea = _currentlyDrawnArea.Union(range);
                    }
                    else
                    {
                        // We get here when the camera instantly jumps to a new position on the timeline.
                        // e.g when we die halfway through a timeline and get re-spawned at the beginning.
                        // In which case, the new area to draw isn't a union with the old area - it's a from scratch draw.
                        Debug.Log($"Discarding _currentlyDrawnArea of {_currentlyDrawnArea} b/c it does not overlap with newly built range: {range}");
                        _currentlyDrawnArea = range;
                    }
                }
            }

            // Cull out the portion of the currently drawn area that is no longer visible.
            _currentlyDrawnArea = _currentlyDrawnArea.Intersection(visibleRange);

            ReturnUnusedObjects();
        }

        private void ReturnUnusedObjects()
        {
            _activeObjects.Where(obj => !_currentlyDrawnArea.Overlaps(obj.Range)).ToList().ForEach(obj => Return(obj));
        }

        // Return true if the build was successful.
        protected virtual UniTask<bool> BuildObjectsIn(TimelineRange range)
        {
            throw new Exception("implement in child class");
        }

        protected override IObservable<T> CreateInstanceAsync()
        {
            return GetPrefab().ToObservable();
        }

        /// <summary>
        /// Called before return to pool, useful for set active object(it is default behavior).
        /// </summary>
        protected override void OnBeforeRent(T instance)
        {
            base.OnBeforeRent(instance);
            _activeObjects.Add(instance);
        }

        /// <summary>
        /// Called before return to pool, useful for set inactive object(it is default behavior).
        /// </summary>
        protected override void OnBeforeReturn(T instance)
        {
            base.OnBeforeReturn(instance);
            _activeObjects.Remove(instance);
        }

        private async UniTask<T> GetPrefab()
        {
            T component = null;
            try
            {
                var prefab = await _config.GetPrefab();
                var thingToBuild = GameObject.Instantiate(prefab, _hierarchyParent, false);
                component = thingToBuild.GetComponent<T>();
                if (component == null)
                {
                    var allComps = thingToBuild.GetComponents<MonoBehaviour>();
                    Debug.Log($"{allComps.Length} comps found, but none of type T");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return component;
        }

        // You can overload OnBeforeRent, OnBeforeReturn, OnClear for customize action.
        // In default, OnBeforeRent = SetActive(true), OnBeforeReturn = SetActive(false)

        // protected override void OnBeforeRent(Foobar instance)
        // protected override void OnBeforeReturn(Foobar instance)
        // protected override void OnClear(Foobar instance)

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _subscriptions?.Dispose();
        }
    }
}
