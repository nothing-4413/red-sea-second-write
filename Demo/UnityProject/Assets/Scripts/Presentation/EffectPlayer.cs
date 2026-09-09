using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using RedSea.Match3.Core;
using RedSea.Match3.Architecture;

namespace RedSea.Match3.Presentation
{
    public sealed class EffectPlayer : MonoBehaviour
    {
        public void Play(TurnResolver resolver, BoardView boardView, TurnPerformanceTrace performance, Action completed, Action<ErrorSnapshot> timedOut)
        {
            StartCoroutine(Consume(resolver, boardView, performance, completed, timedOut));
        }

        public void StopPlayback()
        {
            isPaused = false;
            playback = null;
            StopAllCoroutines();
        }

        private bool isPaused;
        private Stopwatch playback;

        public void SetPaused(bool paused)
        {
            isPaused = paused;
            if (playback == null) return;
            if (paused) playback.Stop(); else playback.Start();
        }

        private IEnumerator Consume(TurnResolver resolver, BoardView boardView, TurnPerformanceTrace performance, Action completed, Action<ErrorSnapshot> timedOut)
        {
            var elapsed = 0f; playback = Stopwatch.StartNew();
            while (resolver.TryConsume(out var item))
            {
                while (isPaused) yield return null;
                var duration = resolver.Board.Config.EventDurationSeconds;
                if (elapsed + duration > resolver.Board.Config.MaxTurnWaitSeconds)
                {
                    var error = resolver.CaptureError(GameErrorType.Presentation, "Presentation event playback timeout.", GameState.Animating, item.TurnId);
                    resolver.Events.Clear();
                    boardView.SyncFromModel();
                    playback.Stop(); performance?.RecordEventPlayback(playback.Elapsed.TotalMilliseconds);
                    playback = null;
                    timedOut?.Invoke(error);
                    yield break;
                }
                boardView.ApplyEvent(item);
                var eventElapsed = 0f;
                while (eventElapsed < duration)
                {
                    if (!isPaused) eventElapsed += Time.deltaTime;
                    yield return null;
                }
                elapsed += eventElapsed;
            }
            boardView.SyncFromModel();
            playback.Stop(); performance?.RecordEventPlayback(playback.Elapsed.TotalMilliseconds);
            playback = null;
            completed?.Invoke();
        }
    }
}
