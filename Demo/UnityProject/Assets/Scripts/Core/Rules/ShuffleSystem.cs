using System;
using System.Collections.Generic;
using System.Linq;
using RedSea.Match3.Core;

namespace RedSea.Match3.Core.Rules
{
    public sealed class ShuffleResult
    {
        public bool IsShuffled;
        public bool UsedRebuild;
        public int Attempts;
    }

    public static class ShuffleSystem
    {
        public static ShuffleResult TryShuffle(BoardModel board, IList<ResolveEvent> events, int turnId)
        {
            var result = new ShuffleResult();
            if (!board.Config.EnableShuffle || MatchFinder.Find(board).Count > 0 || board.HasLegalMove()) return result;

            var before = BoardState.Capture(board);
            var slots = TraversableSlots(board);
            if (slots.Count == 0 || slots.Any(cell => cell.Piece == null)) return result;
            var colors = slots.Select(cell => cell.Piece.Color).ToList();

            for (var attempt = 1; attempt <= board.Config.MaxShuffleAttempts; attempt++)
            {
                result.Attempts = attempt;
                FisherYates(colors, board.ShuffleRandom);
                ApplyColors(board, slots, colors);
                if (IsPlayableStable(board))
                {
                    AddEvent(events, slots, turnId);
                    result.IsShuffled = true;
                    return result;
                }
            }

            for (var attempt = 1; attempt <= board.Config.MaxShuffleAttempts; attempt++)
            {
                result.Attempts++;
                var rebuilt = BuildStableCandidate(board, slots);
                ApplyColors(board, slots, rebuilt);
                if (IsPlayableStable(board))
                {
                    AddEvent(events, slots, turnId);
                    result.IsShuffled = true;
                    result.UsedRebuild = true;
                    return result;
                }
            }

            before.Restore(board);
            throw new InvalidOperationException("Rule error: unable to rebuild a playable board after shuffle limit.");
        }

        private static List<BoardCell> TraversableSlots(BoardModel board)
        {
            var slots = new List<BoardCell>();
            for (var row = 0; row < board.Config.Rows; row++)
                for (var column = 0; column < board.Config.Columns; column++)
                    if (board.Cells[row, column].Obstacle == null) slots.Add(board.Cells[row, column]);
            return slots;
        }

        private static void FisherYates(IList<PieceColor> colors, SeededRandom random)
        {
            for (var index = colors.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                var color = colors[index];
                colors[index] = colors[swapIndex];
                colors[swapIndex] = color;
            }
        }

        private static List<PieceColor> BuildStableCandidate(BoardModel board, IList<BoardCell> slots)
        {
            var result = new List<PieceColor>();
            foreach (var cell in slots)
            {
                var candidates = board.Config.Colors.ToList();
                FisherYates(candidates, board.ShuffleRandom);
                var selected = candidates.FirstOrDefault(color => !MakesLine(board, cell.Pos, color));
                result.Add(selected);
                if (cell.Piece == null) cell.Piece = board.CreatePiece(selected, cell.Pos);
                else cell.Piece.Color = selected;
            }
            return result;
        }

        private static bool MakesLine(BoardModel board, CellPos pos, PieceColor color)
        {
            if (pos.Column >= 2 &&
                SameColor(board.Cells[pos.Row, pos.Column - 1].Piece, color) &&
                SameColor(board.Cells[pos.Row, pos.Column - 2].Piece, color)) return true;
            if (pos.Row >= 2 &&
                SameColor(board.Cells[pos.Row - 1, pos.Column].Piece, color) &&
                SameColor(board.Cells[pos.Row - 2, pos.Column].Piece, color)) return true;
            return false;
        }

        private static bool SameColor(Piece piece, PieceColor color)
        {
            return piece != null && piece.Color == color;
        }

        private static void ApplyColors(BoardModel board, IList<BoardCell> slots, IList<PieceColor> colors)
        {
            for (var index = 0; index < slots.Count; index++)
            {
                var cell = slots[index];
                if (cell.Piece == null) cell.Piece = board.CreatePiece(colors[index], cell.Pos);
                else cell.Piece.Color = colors[index];
                cell.Piece.LogicalPos = cell.Pos;
            }
        }

        private static bool IsPlayableStable(BoardModel board)
        {
            return MatchFinder.Find(board).Count == 0 && board.HasLegalMove();
        }

        private static void AddEvent(IList<ResolveEvent> events, IList<BoardCell> slots, int turnId)
        {
            events.Add(new ResolveEvent(ResolveEventType.Shuffle, turnId)
            {
                AffectedCells = slots.Select(cell => cell.Pos).ToList()
            });
        }
    }
}
