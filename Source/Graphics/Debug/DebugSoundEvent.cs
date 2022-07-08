using UnityEngine;
using Shapes;
using UniRx;

namespace UniBeat.RhythmEngine
{
    public class DebugSoundEvent : DrawableTimelineEvent<SoundEvent>
    {
        public Color Color;
        protected override string DebugTextValue => $"{Event.Asset}";

        protected override void UpdateSize()
        {
            ((Disc)DebugShape).Radius = Width / 2;
        }

        protected override void UpdateColor()
        {
            // do nothing - this causes the disc to become invisible for some reason - prob bug in Shapes.
            //((Disc)DebugShape).Color = Color;
        }

        protected override Color GetColor()
        {
            return Color;
        }
    }
}
