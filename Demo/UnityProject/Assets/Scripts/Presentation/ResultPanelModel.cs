using RedSea.Match3.Core;

namespace RedSea.Match3.Presentation
{
    public sealed class ResultPanelModel
    {
        public bool Visible { get; private set; }
        public GameState State { get; private set; }
        public int Score { get; private set; }
        public int GoalProgress { get; private set; }
        public int GoalTarget { get; private set; }
        public int RemainingMoves { get; private set; }

        public void Hide() { Visible = false; }

        public void Update(BoardModel board, GameState state)
        {
            Visible = state == GameState.Win || state == GameState.Lose;
            State = state;
            if (board == null) return;
            Score = board.Score;
            GoalProgress = board.ClearedByColor[board.Config.GoalColor];
            GoalTarget = board.Config.GoalCount;
            RemainingMoves = board.MovesRemaining;
        }
    }
}
