using System;
using RedSea.Match3.Core;

namespace RedSea.Match3.Flow
{
    public sealed class TurnStateMachine
    {
        public GameState State { get; private set; } = GameState.Idle;
        private GameState stateBeforePause = GameState.Idle;
        public bool InputLocked => State != GameState.Idle && State != GameState.Selecting;
        public bool IsPaused => State == GameState.Paused;
        public event Action<GameState> StateChanged;
        public void Enter(GameState next) { State = next; StateChanged?.Invoke(next); }
        public void Select() { if (State == GameState.Idle || State == GameState.Selecting) Enter(GameState.Selecting); }
        public void BeginResolve() { if (State == GameState.Selecting) Enter(GameState.Resolving); }
        public void BeginAnimation() { if (State == GameState.Resolving || State == GameState.Refilling) Enter(GameState.Animating); }
        public void BeginRefill() { Enter(GameState.Refilling); }
        public void CheckChain() { Enter(GameState.CheckingChain); }
        public void ReturnToIdle() { if (State != GameState.Win && State != GameState.Lose) Enter(GameState.Idle); }
        public void Finish(GameState result) { if (result == GameState.Win || result == GameState.Lose) Enter(result); }
        public bool Pause() { if (IsPaused || State == GameState.Win || State == GameState.Lose) return false; stateBeforePause = State; Enter(GameState.Paused); return true; }
        public bool Resume() { if (!IsPaused) return false; var next = stateBeforePause; stateBeforePause = GameState.Idle; Enter(next); return true; }
        public void Reset() { stateBeforePause = GameState.Idle; Enter(GameState.Idle); }
    }
}
