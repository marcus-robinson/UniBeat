using UnityEngine;
using Shapes;

namespace UniBeat.RhythmEngine
{
    public class DebugTimelineIndex : DrawableTimelineEvent<IndexEvent>
    {
        public Color Color = Color.red;
        public float BeatOverhang = 5f;
        public bool IsBeat => Index % 4 == 0;
        public bool IsBar => Index % 16 == 0;

        protected override void UpdatePosition()
        {
            base.UpdatePosition();
            DebugText.gameObject.transform.localPosition = new Vector3(0.2f, Index % 2, 1f);
        }

        protected override void UpdateSize()
        {
            var bounds = _timeline.Collider.bounds;

            var lineStart = -bounds.extents.y - (IsBeat ? BeatOverhang : 0) - (IsBar ? BeatOverhang : 0);
            var lineEnd = bounds.extents.y + (IsBeat ? BeatOverhang : 0) + (IsBar ? BeatOverhang : 0);
            ((Line)DebugShape).Start = new Vector3(0, lineStart, 0);
            ((Line)DebugShape).End = new Vector3(0, lineEnd, 0);
        }

        protected override Color GetColor() => Color;
    }
}
