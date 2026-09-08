using RedSea.Match3.Core;

namespace RedSea.Match3.Architecture
{
    public sealed class GoalSystem
    {
        public bool IsCompleted(BoardModel board) { return board.ClearedByColor[board.Config.GoalColor] >= board.Config.GoalCount; }
        public bool IsFailed(BoardModel board) { return board.MovesRemaining <= 0 && !IsCompleted(board); }
    }

    public sealed class ScoreSystem
    {
        public int ScoreDelta(ResolveSummary summary) { return summary == null ? 0 : summary.ScoreDelta; }
    }
}
