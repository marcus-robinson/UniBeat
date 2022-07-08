using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

/***
* Extends the base Timeline class and adds functionality for rendering a timeline to screen.
*/
namespace UniBeat.RhythmEngine
{
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class DrawableTimeline : TimelineWithPlaybackControl, ITimelineDrawableWithPlaybackControl
    {
        public float Speed => _config.Speed; //unity worldspace units per second
        public float VisibleAreaBleed => _config.VisibleAreaBleed; //unity worldspace units
        protected float _calculatedWidth => WidthOfRange(PlaybackRegion);
        public BoxCollider2D Collider { get; private set; }
        public TimelineDirection Direction => _config.Direction;
        public IObservable<TimelineRange> VisibleArea => _visibleArea.AsObservable();
        private readonly Subject<TimelineRange> _visibleArea = new Subject<TimelineRange>();
        private readonly Dictionary<int, float> _positionOfIndex = new Dictionary<int, float>();

        public TimelinePlayhead GetPlayhead()
        {
            return GetComponentInChildren<TimelinePlayhead>();
        }

        protected override void Initialize()
        {
            base.Initialize();
            BuildPositionCache();
            GenerateBoundsForTempos();
            ResizeCollider();
        }

        private void ResizeCollider()
        {
            Collider = this.gameObject.GetComponent<BoxCollider2D>();
            Collider.size = new Vector2(_calculatedWidth, _config.Height);
            Collider.offset = new Vector2((int)Direction * _calculatedWidth / 2, 0);
        }

        private void BuildPositionCache()
        {
            _positionOfIndex.Clear();
            // Quantize the events with their messy indexes into a clean integer map
            // If our playback region extends past the end of the data, that's fine - it's just a silent section.
            for (var i = 0; i <= Math.Max(_data.Size.EndAsInt, PlaybackRegion.EndAsInt); i++)
            {
                var width = WidthOfRange___Internal(new TimelineRange(_data.Size.Start, i));
                _positionOfIndex.Add(i, width); //cache the world space position of every sixteenth index
            }
        }

        private void GenerateBoundsForTempos()
        {
            var events = _data.Events.OfType<TempoEvent>().ToList();
            events.ForEach(e =>
            {
                var start = PositionOf(e.Index);
                var width = (float)e.WidthAtAbsoluteIndex(Speed, e.EndIndex ?? Math.Max(_data.Size.End, PlaybackRegion.End));
                e.Bounds = new Bounds(
                    new Vector3((start + (int)Direction * width / 2), 0, 0),
                    new Vector3(width, _config.Height, 0)
                );
            });
        }

        protected override void SubscribeToUnityHooks()
        {
            base.SubscribeToUnityHooks();

            // Add ObservableXxxTrigger components so we can handle our Update/FixedUpdate hooks as Observables
            var updates = this.UpdateAsObservable();

            // Do Visible Area updates every 60 frames
            updates.SampleFrame(_config.VisibleAreaSampleRate).Subscribe(_ => _visibleArea.OnNext(GetVisibleArea()));
        }

        // Given a sixteenth index on the timeline, return the local x position
        // if the Direction is left-to-right the return value will be positive, otherwise negative.
        // The value of index will be clamped if it's outside the bounds of the timeline.
        public float PositionOf(double index)
        {
            var previousIndex = (int)index;
            var nextIndex = Math.Min(previousIndex + 1, Math.Max(_data.Size.EndAsInt, PlaybackRegion.EndAsInt));
            var xPrior = PositionOf(previousIndex);
            var xNext = PositionOf(nextIndex);
            return Mathf.Lerp(xPrior, xNext, 1f - (float)(nextIndex - index));
        }

        public float PositionOf(int index)
        {
            // if (index < 0 || (!PlaybackRegion.Contains(index) && !_data.Size.Contains(index)))
            // {
            //     Debug.LogWarning($"{index} is outside this Timeline's range, so we're clamping it to a valid value");

            //     // We deliberately don't round here, since we don't round when we quantize the timeline and cache w the _positionOfIndex map.
            //     // Therefore, if the End value is 5.7, the final quanitzed index in the Timeline will be 5.
            //     index = (int)Mathf.Clamp((float)index, 0f, (float)Math.Max(_data.Size.EndAsInt, PlaybackRegion.EndAsInt));
            // }

            return (int)Direction * (_positionOfIndex[index] - _positionOfIndex[PlaybackRegion.StartAsInt]);
        }

        // Given a local x position on the timeline, return the sixteenth index
        // if the Direction is left-to-right the localPositionX param must be positive, otherwise negative.
        // localPositionX is clamped to the start of the timeline, so the returned Index always
        // is greater than or equal to PlaybackRegion.Start, by may be bigger than PlaybackRegion.End
        public double IndexOf(float localPositionX)
        {
            // Clamp the position to ensure it's not before the timeline starts,
            // but allow it to be unbounded in the direction of the timeline.
            if (Direction.IsLeftToRight())
                localPositionX = Math.Max(localPositionX, 0);
            else
                localPositionX = Math.Min(localPositionX, 0);

            var absolutePositionX = localPositionX + (int)Direction * _positionOfIndex[PlaybackRegion.StartAsInt];

            for (var i = PlaybackRegion.StartAsInt; i <= Math.Max(_data.Size.EndAsInt, PlaybackRegion.EndAsInt) - 1; i++)
            {
                if (_positionOfIndex[i] <= Math.Abs(absolutePositionX) && _positionOfIndex[i + 1] >= Math.Abs(absolutePositionX))
                {
                    var exactIndex = Mathf.Lerp(i, i + 1, (Math.Abs(absolutePositionX) - _positionOfIndex[i]) / (_positionOfIndex[i + 1] - _positionOfIndex[i]));
                    return exactIndex;
                }
            }

            // We must be beyond the end of the timeline, so we return the last valid index.
            return Math.Max(_data.Size.End, PlaybackRegion.End);

            // From here, we can only approximate an index.
            // Do we really need this? Could just clamp things and return this finalIndex value here...
            // var finalIndex = (int)Math.Max(_data.Size.EndAsInt, PlaybackRegion.EndAsInt);
            // var finalPos = _positionOfIndex[finalIndex];
            // var overrun = Math.Abs(absolutePositionX) - finalPos;

            // var finalTempo = _data.Events.OfType<TempoEvent>().Last();
            // var finalTempoIndexSize = Math.Max(_data.Size.End, PlaybackRegion.End) - finalTempo.Index;
            // var approxIndex = finalIndex + Mathf.Abs(overrun / finalTempo.Bounds.size.x) * finalTempoIndexSize;
            // return approxIndex;
        }

        public float WidthOfRange(TimelineRange range)
        {
            // Shift the range to ensure it stays within timline bounds
            var actual = PlaybackRegion.Intersection(range);
            return Math.Abs(PositionOf(actual.End)) - Math.Abs(PositionOf(actual.Start));
        }

        // Warning, this method isn't always accurate, since the final sixteenth beat
        // in a music timing may be cut short if the next music timing falls on a non-integer index.
        // For this reason, do not use this method for precise calculations - use `WidthOfRange` instead.
        public float WidthOfASixteenthBeatAt(double index)
        {
            if (!PlaybackRegion.Contains(index))
                throw new Exception("Invalid index - outside of the timeline's playback regions");

            var tempo = _data.TempoAt((int)Math.Round(index));
            var start = Math.Max(PlaybackRegion.Start, tempo.Index);
            return WidthOfRange(new TimelineRange(start, start + 1d));
        }

        // Using the formula: speed = distance / time
        // we calculate the "distance" (width) of this timeline range with params:
        // a) speed (player run speed - this is constant)
        // b) time (duration of the this timeline range)
        // the second point is tricky. It's constant at the index level, but dynamic at the beat / range level,
        // since the duration of beats changes based on the tempo changes throughout the timeline.
        private float WidthOfRange___Internal(TimelineRange range)
        {
            var width = 0d;

            var timings = _data.InRange<TempoEvent>(range);
            timings.ForEach(e =>
            {
                var endPoint = range.End;
                if (e.EndIndex != null)
                {
                    if (range.End > e.EndIndex)
                    {
                        endPoint = (double)e.EndIndex;
                    }
                }

                width += e.WidthAtAbsoluteIndex(Speed, endPoint);
            });

            return (float)width;
        }

        //////////////////////////////////////////

        private TimelineRange GetVisibleArea()
        {
            var cameraX = Camera.main.transform.position.x;
            var visibleHeight = Camera.main.orthographicSize * 2f;
            var visibleWidth = visibleHeight * Screen.width / Screen.height;
            var worldPosLeft = cameraX - visibleWidth / 2 - _config.VisibleAreaBleed;
            var worldPosRight = cameraX + visibleWidth / 2 + _config.VisibleAreaBleed;
            var localPosLeft = transform.InverseTransformPoint(new Vector3(worldPosLeft, 0, 0));
            var localPosRight = transform.InverseTransformPoint(new Vector3(worldPosRight, 0, 0));
            var leftIndex = IndexOf(localPosLeft.x);
            var rightIndex = IndexOf(localPosRight.x);

            // Debug.Log($"Left: {leftIndex} | Right: {rightIndex} \n worldPosLeft: {worldPosLeft} | worldPosRight: {worldPosRight} | localPosLeft: {localPosLeft.x} | localPosRight: {localPosRight.x}");

            if (Direction.IsLeftToRight())
                return new TimelineRange(leftIndex, rightIndex);
            else
                return new TimelineRange(rightIndex, leftIndex);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _visibleArea.OnCompleted();
            _visibleArea.Dispose();
        }
    }
}
