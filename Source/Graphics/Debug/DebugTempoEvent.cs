using System;
using System.Linq;
using UnityEngine;
using Shapes;
using UniRx;

namespace UniBeat.RhythmEngine
{
    public class DebugTempoEvent : DrawableTimelineEvent<TempoEvent>
    {
        public override float Width => Range.End > _timeline.PlaybackRegion.End ? _timeline.WidthOfRange(new TimelineRange(Event.Index, _timeline.PlaybackRegion.End)) : Event.Bounds.size.x;
        public override float Height => _timeline.Collider.bounds.size.y;
        public override TimelineRange Range => Event.IsFinal ? new TimelineRange(Event.Index, _timeline.PlaybackRegion.End + 1) : Event.Range;
        protected override string DebugTextValue => $"Range: {Event.Index} - {Event.EndIndex?.ToString("F2") ?? "END OF SONG"} | BPM: {(int)Math.Round(Event.Tempo.BeatsPerMinute)}";
        private Color _inactiveColor = Color.gray;
        private Color _activeColor = Color.gray;
        private float _verticalOffset;

        public override void Draw(TempoEvent e, ITimelineDrawable timeline)
        {
            base.Draw(e, timeline);

            _activeColor = UnityEngine.Random.ColorHSV();
            _inactiveColor = new Color(_activeColor.a, _activeColor.g, _activeColor.b, 0.5f);
            _verticalOffset = UnityEngine.Random.Range(-2, 2);

            _timeline.Init.Subscribe(data =>
            {
                Redraw();
                gameObject.name = $"Tempo - {StartIndex}";
            });

            // Redraw on every sixteenth index change or onSeek
            _timeline.SixteenthIndex.Merge(
                _timeline.OnSeek.Select<double, int>(val => (int)Math.Round(val)) // convert seek double to int's
                ).Subscribe(_ => Redraw()).AddTo(_subscriptions);
        }

        protected override Color GetColor()
        {
            if (_timeline.CurrentIndex >= StartIndex && _timeline.CurrentIndex <= EndIndex)
            {
                return _activeColor;
            }

            return _inactiveColor;
        }

        protected override void UpdateSize()
        {
            ((Rectangle)DebugShape).Width = Width;
            ((Rectangle)DebugShape).Height = Height;
        }

        protected override void UpdateText()
        {
            base.UpdateText();
            DebugText.gameObject.transform.localPosition = new Vector3(
                (int)_timeline.Direction * (-Width / 2 + DebugText.rectTransform.rect.width / 2),
                -Height / 2,
                1f);
        }

        protected override void UpdatePosition()
        {
            // We must draw based on the Event.Bounds.min / max values, because
            // if this is just one of many Music Timings within the middle of this Timeline
            // then we render the entire thing based on its bounds.
            // Otherwise, if its the first or last, then we are probably rendering a subsection of the music timing,
            // in which case our width and position is constrained by the end of
            // the timeline. All this complexity is already taken into account in the Event.Bounds.
            var startPos = _timeline.Direction.IsLeftToRight() ? Event.Bounds.min.x : Event.Bounds.max.x;
            transform.position = _timeline.transform.TransformPoint(new Vector3(
                startPos + (int)_timeline.Direction * Width / 2,
                Event.Bounds.center.y + _verticalOffset,
                0));
        }
    }
}
