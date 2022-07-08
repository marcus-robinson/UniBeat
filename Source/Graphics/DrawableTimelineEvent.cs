using System;
using Shapes;
using UniRx;
using UnityEngine;

namespace UniBeat.RhythmEngine
{
    // Draw a thing whenever an event of type T appears on the timeline.
    public abstract class DrawableTimelineEvent<T> : MonoBehaviour
        where T : ITimelineEvent
    {
        ///////////////////////////
        // Unity Inspector Properties
        ///////////////////////////
        // The debug shape and text are so we always have an easy way to visualise events when a Timeline is in debug mode.
        public ShapeRenderer DebugShape;
        public TMPro.TextMeshPro DebugText;
        public float Scale = 1f;

        ///////////////////////////
        // Public Accessors ///////
        public T Event => _event;
        public virtual int Index => _event.IndexAsInt;
        public virtual int StartIndex => Range.StartAsInt;
        public virtual int EndIndex => Range.EndAsInt;
        public virtual float Width => _timeline.WidthOfASixteenthBeatAt(Range.Start) * (float)Math.Max(Range.Size, 1) * Scale;
        public virtual float Height => _timeline.WidthOfASixteenthBeatAt(Range.Start) * Scale;
        public virtual TimelineRange Range => Event.Range;
        ///////////////////////////

        protected Vector3 _positionInTimeline;
        protected Vector3 _initialPosition;
        protected virtual Vector3 Position => new Vector3(_positionInTimeline.x, _initialPosition.y + Event.VerticalOffset, 0);
        protected virtual string DebugTextValue => $"{Event.IndexAsInt}";
        protected ITimelineDrawable _timeline;
        protected CompositeDisposable _subscriptions;
        private T _event;

        public virtual void Draw(T e, ITimelineDrawable timeline)
        {
            BeforeDraw();

            _event = e;
            _timeline = timeline;
            gameObject.name = $"{GetType().Name} - {StartIndex}";

            _subscriptions?.Dispose(); // If we're being recycled
            _subscriptions = new CompositeDisposable();

            // show/hide debug text when the timeline is in debug mode
            _timeline.DebugMode.Subscribe(value => OnDebugChange(value)).AddTo(_subscriptions);

            // Reset our position.
            transform.localPosition = new Vector3(0, 0, 0);
            // Save our coordinates.
            _positionInTimeline = _timeline.transform.TransformPoint(new Vector3(_timeline.PositionOf(StartIndex), 0, 0));
            _initialPosition = transform.position;

            Redraw();
        }

        protected virtual void OnDebugChange(bool debug)
        {
            DebugText.enabled = DebugShape.enabled = debug;
        }

        protected virtual void BeforeDraw() { }

        protected virtual void Redraw()
        {
            UpdatePosition();
            UpdateColor();
            UpdateSize();
            UpdateText();
        }

        protected abstract Color GetColor();
        protected abstract void UpdateSize(); // Be sure to update your debug shape size in here.

        protected virtual void UpdatePosition()
        {
            transform.position = Position;
        }

        protected virtual void UpdateText()
        {
            DebugText.text = DebugTextValue;
        }

        protected virtual void UpdateColor()
        {
            DebugShape.Color = GetColor();
        }

        // When returned to the pool, be sure to kill any subscriptions.
        void OnDisable()
        {
            _subscriptions?.Dispose();
        }

        void OnDestroy()
        {
            _subscriptions?.Dispose();
        }
    }
}
