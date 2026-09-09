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
            StopAllCoroutines();
        }

        private IEnumerator Consume(TurnResolver resolver, BoardView boardView, TurnPerformanceTrace performance, Action completed, Action<ErrorSnapshot> timedOut)
        {
            var elapsed = 0f; var playback = Stopwatch.StartNew();
            while (resolver.TryConsume(out var item))
            {
                var duration = resolver.Board.Config.EventDurationSeconds;
                if (elapsed + duration > resolver.Board.Config.MaxTurnWaitSeconds)
                {
                    var error = resolver.CaptureError(GameErrorType.Presentation, "Presentation event playback timeout.", GameState.Animating, item.TurnId);
                    resolver.Events.Clear();
                    boardView.SyncFromModel();
                    playback.Stop(); performance?.RecordEventPlayback(playback.Elapsed.TotalMilliseconds);
                    timedOut?.Invoke(error);
                    yield break;
                }
                boardView.ApplyEvent(item);
                yield return new WaitForSeconds(duration);
                elapsed += duration;
            }
            boardView.SyncFromModel();
            playback.Stop(); performance?.RecordEventPlayback(playback.Elapsed.TotalMilliseconds);
            completed?.Invoke();
        }
    }
}
