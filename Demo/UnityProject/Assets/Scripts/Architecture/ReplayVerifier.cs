using System;
using System.Linq;
using RedSea.Match3.Core;

namespace RedSea.Match3.Architecture
{
    public sealed class ReplayVerificationResult
    {
        public bool IsMatch;
        public int FailedInputIndex = -1;
        public string Message;
    }

    public static class ReplayVerifier
    {
        public static ReplayVerificationResult Verify(LevelConfig config, ReplayRecord record)
        {
            if (record == null) return Failure(-1, "Replay record is null.");
            if (!record.IsCompatible(ReplayRecord.RuleVersion)) return Failure(-1, "Replay rule version is incompatible.");
            if (record.Summaries.Count != record.Inputs.Count) return Failure(-1, "Replay input and summary counts differ.");

            var replayConfig = config.Clone();
            replayConfig.Seed = record.Seed;
            var resolver = new TurnResolver(new BoardModel(replayConfig));
            if (!RestoreSnapshot(resolver.Board, record.InitialSnapshot)) return Failure(-1, "Initial board snapshot is invalid.");
            resolver.Board.RefillRandom.Restore(resolver.Board.RefillRandom.State, record.InitialRefillRandomIndex);
            resolver.Board.ShuffleRandom.Restore(resolver.Board.ShuffleRandom.State, record.InitialShuffleRandomIndex);

            for (var index = 0; index < record.Inputs.Count; index++)
            {
                var input = record.Inputs[index];
                var expected = record.Summaries[index];
                var result = input.Type == ReplayInputType.AreaTool
                    ? resolver.SubmitAreaTool(input.Center, expected.TurnId)
                    : resolver.SubmitSwap(input.From, input.To, expected.TurnId);
                Drain(resolver);
                if (!result.IsValid || !Matches(result.Summary, expected)) return Failure(index, "Resolve summary differs.");
            }

            return new ReplayVerificationResult { IsMatch = true, Message = "Replay verified." };
        }

        private static bool Matches(ResolveSummary actual, ResolveSummary expected)
        {
            return actual != null &&
                actual.TurnId == expected.TurnId &&
                actual.ScoreDelta == expected.ScoreDelta &&
                actual.ChainCount == expected.ChainCount &&
                actual.RemainingMoves == expected.RemainingMoves &&
                actual.RemainingAreaTools == expected.RemainingAreaTools &&
                actual.DestroyedObstacles == expected.DestroyedObstacles &&
                SameColorCounts(actual, expected) &&
                actual.FinalSnapshot == expected.FinalSnapshot;
        }

        private static bool SameColorCounts(ResolveSummary actual, ResolveSummary expected)
        {
            if (actual.ClearedByColor == null || expected.ClearedByColor == null) return actual.ClearedByColor == expected.ClearedByColor;
            return actual.ClearedByColor.Count == expected.ClearedByColor.Count &&
                actual.ClearedByColor.All(pair => expected.ClearedByColor.TryGetValue(pair.Key, out var count) && count == pair.Value);
        }

        private static void Drain(TurnResolver resolver)
        {
            while (resolver.TryConsume(out _)) { }
        }

        private static bool RestoreSnapshot(BoardModel board, string snapshot)
        {
            var rows = snapshot.Split(new[] { "\n" }, StringSplitOptions.None);
            if (rows.Length != board.Config.Rows || rows.Any(row => row.Length != board.Config.Columns)) return false;
            for (var row = 0; row < board.Config.Rows; row++)
                for (var column = 0; column < board.Config.Columns; column++)
                {
                    var symbol = rows[row][column];
                    var cell = board.Cells[row, column];
                    if (symbol == 'X')
                    {
                        if (cell.Obstacle == null) return false;
                        cell.Piece = null;
                    }
                    else if (symbol == '.')
                    {
                        if (cell.Obstacle != null) return false;
                        cell.Piece = null;
                    }
                    else
                    {
                        if (cell.Obstacle != null) return false;
                        cell.Piece = board.CreatePiece(ParseColor(symbol), cell.Pos);
                    }
                }
            return board.Snapshot() == snapshot;
        }

        private static PieceColor ParseColor(char symbol)
        {
            return symbol == 'R' ? PieceColor.Red : symbol == 'B' ? PieceColor.Blue : symbol == 'G' ? PieceColor.Green : symbol == 'Y' ? PieceColor.Yellow : PieceColor.Purple;
        }

        private static ReplayVerificationResult Failure(int index, string message)
        {
            return new ReplayVerificationResult { IsMatch = false, FailedInputIndex = index, Message = message };
        }
    }
}
