using RedSea.Match3.Core;

namespace RedSea.Match3.Architecture
{
    public sealed class TurnResolver
    {
        public BoardModel Board { get; private set; }
        public EventQueue Events { get; private set; }
        public GoalSystem Goals { get; private set; }
        public ScoreSystem Scores { get; private set; }
        public ReplayRecord Replay { get; private set; }

        public TurnResolver(BoardModel board)
        {
            Board = board; Events = new EventQueue(board.Config.MaxEvents); Goals = new GoalSystem(); Scores = new ScoreSystem();
            Replay = new ReplayRecord { Seed = board.Config.Seed, InitialSnapshot = board.Snapshot() };
        }

        public ResolveResult SubmitSwap(CellPos from, CellPos to, int turnId)
        {
            var result = ResolveSystem.Swap(Board, from, to, turnId);
            if (!result.IsValid) return result;
            Events.EnqueueRange(result.Events); Replay.Inputs.Add(new ReplayInput(from, to)); Replay.Summaries.Add(result.Summary); return result;
        }

        public ResolveResult SubmitAreaTool(CellPos center, int turnId)
        {
            var result = ResolveSystem.AreaTool(Board, center, turnId);
            if (!result.IsValid) return result;
            Events.EnqueueRange(result.Events); Replay.Summaries.Add(result.Summary); return result;
        }

        public bool TryConsume(out ResolveEvent item) { return Events.TryDequeue(out item); }
        public bool TryFindLegalMove(out CellPos from, out CellPos to) { for (var row = 0; row < Board.Config.Rows; row++) for (var column = 0; column < Board.Config.Columns; column++) { var candidate = new CellPos(row, column); foreach (var neighbor in Board.Neighbors(candidate)) if (SwapValidator.CanSwap(Board, candidate, neighbor)) { from = candidate; to = neighbor; return true; } } from = new CellPos(); to = new CellPos(); return false; }
        public GameState EvaluateEndState() { return Goals.IsCompleted(Board) ? GameState.Win : Goals.IsFailed(Board) ? GameState.Lose : GameState.Idle; }
    }
}
