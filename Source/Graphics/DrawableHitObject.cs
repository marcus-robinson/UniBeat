using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Shapes;

namespace UniBeat.RhythmEngine
{
    public abstract class DrawableHitObject<K> : DrawableTimelineEvent<K>
        where K : HitObjectEvent
    {
        [Space(10)]
        [Header("Hit Result Colors")]
        public Color MistakeHitColor = Color.gray;
        public Color PassHitColor = Color.gray;
        public Color GreatHitColor = Color.gray;

        public bool HasResult => _result?.HasResult ?? false;

        protected override string DebugTextValue => $"{Event.IndexAsInt}";//\n{Event.StartTime.TotalMilliseconds}ms";

        // Miss window is 1ms more than the biggest valid hit window
        public TimeSpan MissWindow => Event.BiggestHitWindow.Add(TimeSpan.FromMilliseconds(1));

        protected Judgement _result => Event.Result;

        public override void Draw(K e, ITimelineDrawable timeline)
        {
            base.Draw(e, timeline);

            e.Results.Subscribe(HandleResult).AddTo(_subscriptions);

            // After our index has been emitted, wait for the duration of the Miss window, and then update the result.
            // This captures the case where the player doesn't provide any input at all and we slide past the playhead.
            e.Emissions.Delay(MissWindow).Subscribe(HandleNoHit).AddTo(_subscriptions);

            // throttle to help with back pressure - seems like it can get double emitted?
            //e.Emissions.Throttle(TimeSpan.FromMilliseconds(10)).Subscribe(HandleEmit).AddTo(_subscriptions);
            e.Emissions.Subscribe(HandleEmit).AddTo(_subscriptions);

            Redraw();
        }

        protected override Color GetColor()
        {
            if (HasResult)
            {
                if (_result.IsMistake)
                {
                    return MistakeHitColor;
                }
                if (_result.Type >= HitResult.Great)
                {
                    return GreatHitColor;
                }
                else if (_result.IsPass)
                {
                    return PassHitColor;
                }
            }
            return Color.black;
        }

        protected virtual void HandleResult(Judgement judgement)
        {
            // Debug.Log($"Index: {Event.IndexAsInt} | JUDGEMENT: {judgement.Type} with offset: {judgement.TimeOffset}ms");

            // [MR] remove this because when player dies, we reset all events and they need to re-draw
            // if (!judgement.HasResult)
            //     return;

            Redraw();
        }

        protected virtual void HandleEmit(double index)
        {
            Debug.Log($"EMITTED: {Event.IndexAsInt} @ timeline index of {_timeline.CurrentIndex}");
        }

        private void HandleNoHit(double index)
        {
            if (Event.Judged)
                return;

            // Debug.Log($"Index: {Event.IndexAsInt} | NO HIT! @ {MissWindow.TotalMilliseconds}ms past when we were expected to be hit.");

            // In an ideal world, we only get here when we're guaranteed a "miss",
            // since we've waited for the MissWindow to elapse. HOWEVER, since events are quantized when emitted
            // it might be that for songs with a low BPM and hitobjects which aren't right on the beat (e.g Bohemian Rhapsody)
            // you may have your event emitted much EARLIER than you expect (as in a full 20ms earlier), which means the full "MissWindow"
            // hasn't actually elapsed, and it's still possible for the user to make a hit, in which case, the following call to "UpdateResult"
            // will NOT result in a judgement.
            Event.UpdateResult(false, _timeline);

            // Which is why we have this safety net. We attempt to get a judgement every fixed update until we're successful
            // (i.e either we get a "Miss" or the user manages to sneak in a hit if we're a little early thanks to aforementioned quantizing).
            this.FixedUpdateAsObservable().TakeWhile(_ => !Event.Judged).Subscribe(_ => Event.UpdateResult(false, _timeline));
        }

        protected override void UpdateSize()
        {
            var size = CalculateShapeSize();
            ((Rectangle)DebugShape).Width = size.x;
            ((Rectangle)DebugShape).Height = size.y;
        }

        protected virtual Vector2 CalculateShapeSize()
        {
            // If the width of the hit object is bigger than the miss window, it's visually deceptive and hard to hit.
            // This only happens at very slow BPM's.
            // In this circumstance, we reduce the width down to the size of the miss window.
            var width = Width;
            var height = Height;
            var durationOfSixteenth = _timeline.Data.TempoAt(Event.IndexAsInt).Tempo.BeatTimeSpan.TotalMilliseconds / Utils.SixteenthsPerBeat;
            var missToIndexRatio = (float)(MissWindow.TotalMilliseconds / durationOfSixteenth);
            // Debug.Log($"durationOfSixteenth: {durationOfSixteenth}ms || Miss Window: {MissWindow.TotalMilliseconds}ms || missToIndexRatio: {missToIndexRatio.ToString("F2")}");

            if (missToIndexRatio < 1)
            {
                width = Width * missToIndexRatio;
                height = width;
                Debug.LogWarning($"{Event.IndexAsInt} has its width reduced from {Width} to {width} because durationOfSixteenth is {durationOfSixteenth}ms which is smaller than the miss window ({MissWindow.TotalMilliseconds}ms)");
            }

            return new Vector2(width, height);
        }

    }
}
