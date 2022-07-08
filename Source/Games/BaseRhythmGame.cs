using System;
using System.Linq;
using UniRx;
using UniBeat.Lib;

namespace UniBeat.RhythmEngine
{
    // "Games" consist solely of handling player inputs and then
    // updating events on the timeline with hit data resulting from the player input.
    // They are NOT like listeners - they don't listen for timeline events - they listen for player input instead.
    // All rendering, sounds, graphics, etc is handled by ObjectPools and DrawableTimelineEvents and driven by the
    // event data of the timeline.
    public abstract class BaseRhythmGame : IDisposable
    {
        protected ITimelinePlaybackControl _timeline;
        protected TimelineData _data;
        protected bool _isRunning;
        protected ScoreProcessor _scoreProcessor;
        protected HealthProcessor _healthProcessor;
        protected CompositeDisposable _subscriptions;
        private IDisposable _buttonsSub;

        /// <summary>
        /// Creates a <see cref="ScoreProcessor"/> for this <see cref="Ruleset"/>.
        /// </summary>
        /// <returns>The score processor.</returns>
        public virtual ScoreProcessor CreateScoreProcessor() => new ScoreProcessor();

        /// <summary>
        /// Creates a <see cref="HealthProcessor"/> for this <see cref="Ruleset"/>.
        /// </summary>
        /// <returns>The health processor.</returns>
        public virtual HealthProcessor CreateHealthProcessor() => new HealthProcessor();

        public BaseRhythmGame(ITimelinePlaybackControl timeline)
        {
            _timeline = timeline;

            _timeline.Init.Subscribe(data =>
            {
                _subscriptions = new CompositeDisposable();
                _data = data;

                _timeline.Active.Where(val => val).Subscribe(_ => StartGame()).AddTo(_subscriptions);
                _timeline.Active.Where(val => !val).Subscribe(_ => PauseGame()).AddTo(_subscriptions);

                _healthProcessor = CreateHealthProcessor();
                _scoreProcessor = CreateScoreProcessor();

                _healthProcessor.SetTimeline(_timeline);
                _scoreProcessor.SetTimeline(_timeline);

                data.Events.OfType<HitObjectEvent>().ToList().ForEach(e =>
                {
                    e.Results.Subscribe(r => HandleResult(r)).AddTo(_subscriptions);
                    e.Difficulty.Value = _timeline.Data.BaseDifficulty;
                    e.InputLatency = _timeline.InputLatency;
                });

                var sampleEvent = data.Events.OfType<HitObjectEvent>().First();

                // End the game after we've given a chance to process any hit objects on the final index.
                // Take into account input latency, as well as a single frame to make sure we don't step on any toes.
                var endGameWindow = sampleEvent.BiggestHitWindow
                    .Add(_timeline.InputLatency)
                    .Add(TimeSpan.FromSeconds(UnityEngine.Time.fixedTime));

                _timeline.Progress.Last().Delay(endGameWindow).Subscribe(_ => EndGame()).AddTo(_subscriptions);
            });
        }

        protected virtual void HandleResult(Judgement result)
        {
            _healthProcessor.ApplyResult(result);
            _scoreProcessor.ApplyResult(result);
        }

        protected virtual void OnButtonPress(ButtonEvent e) { }

        protected virtual void StartGame()
        {
            if (_isRunning)
                return;

            _isRunning = true;

            if (_timeline.InputStyle == InputStyle.TriggerHitOnButtonPressed)
            {
                _buttonsSub = _timeline.ButtonWasPressed.Subscribe(e => OnButtonPress(e));
            }
            else
            {
                _buttonsSub = _timeline.ButtonWasReleased.Subscribe(e => OnButtonPress(e));
            }

        }

        protected virtual void PauseGame()
        {
            _isRunning = false;
            _buttonsSub?.Dispose();
        }

        protected virtual void EndGame()
        {
            var score = new Score();
            _scoreProcessor.PopulateScore(score.ScoreResult);
            UnityEngine.Debug.Log($"{score.ScoreResult}");
            _buttonsSub?.Dispose();
            _subscriptions?.Dispose();
            _isRunning = false;
            _timeline.Pause();
            _timeline.Active.Value = false;
        }

        public void Dispose()
        {
            _buttonsSub?.Dispose();
            _subscriptions?.Dispose();
        }
    }
}
