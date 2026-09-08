using System;
using System.Collections.Generic;
using System.Linq;
using RedSea.Match3.Core;

namespace RedSea.Match3.Core.Rules
{
    public static class GravitySystem
    {
        public static void Apply(BoardModel board, IList<ResolveEvent> events, int turnId)
        {
            for (var column = 0; column < board.Config.Columns; column++)
            {
                var segmentStart = 0;
                for (var row = 0; row <= board.Config.Rows; row++)
                {
                    if (row < board.Config.Rows && board.Cells[row, column].Obstacle == null) continue;
                    CompactSegment(board, column, segmentStart, row - 1, events, turnId);
                    segmentStart = row + 1;
                }
            }
        }

        private static void CompactSegment(BoardModel board, int column, int top, int bottom, IList<ResolveEvent> events, int turnId)
        {
            if (top > bottom) return;
            var pieces = new List<Piece>();
            for (var row = bottom; row >= top; row--) if (board.Cells[row, column].Piece != null) pieces.Add(board.Cells[row, column].Piece);
            for (var row = top; row <= bottom; row++) board.Cells[row, column].Piece = null;
            var targetRow = bottom;
            foreach (var piece in pieces)
            {
                var from = piece.LogicalPos; var target = new CellPos(targetRow, column); board.Cells[targetRow, column].Piece = piece; piece.LogicalPos = target;
                if (from != target) events.Add(new ResolveEvent(ResolveEventType.Fall, turnId) { SourcePieceId = piece.PieceId, TargetCell = target, AffectedCells = new List<CellPos> { from, target } });
                targetRow--;
            }
            while (targetRow >= top)
            {
                var target = new CellPos(targetRow, column); var piece = board.CreatePiece(board.Config.Colors[board.RefillRandom.Next(board.Config.Colors.Length)], target); board.Cells[targetRow, column].Piece = piece;
                events.Add(new ResolveEvent(ResolveEventType.Refill, turnId) { SourcePieceId = piece.PieceId, TargetCell = target, PieceColor = piece.Color }); targetRow--;
            }
        }
    }

    public static class MvpRulePipeline
    {
        public static ResolveResult ResolveSwap(BoardModel board, CellPos from, CellPos to, int turnId)
        {
            if (!SwapValidator.CanSwap(board, from, to)) return Invalid(board, turnId);
            var before = BoardState.Capture(board); var scoreBefore = board.Score; var events = new List<ResolveEvent>();
            SwapPieces(board, from, to); board.MovesRemaining--; events.Add(new ResolveEvent(ResolveEventType.Swap, turnId) { AffectedCells = new List<CellPos> { from, to } });
            return ResolveUntilStable(board, turnId, events, before, scoreBefore, null, 1);
        }

        public static ResolveResult ResolveAreaTool(BoardModel board, CellPos center, int turnId)
        {
            if (board.AreaToolsRemaining <= 0 || !board.Config.IsInBounds(center)) return Invalid(board, turnId);
            var pieceCells = new List<CellPos>(); var obstacleCells = new HashSet<CellPos>();
            for (var row = center.Row - 2; row <= center.Row + 2; row++) for (var column = center.Column - 2; column <= center.Column + 2; column++)
            {
                var pos = new CellPos(row, column); if (!board.Config.IsInBounds(pos)) continue; var cell = board.Cell(pos);
                if (cell.Piece != null) pieceCells.Add(pos); if (cell.Obstacle != null) obstacleCells.Add(pos);
            }
            if (pieceCells.Count == 0 && obstacleCells.Count == 0) return Invalid(board, turnId);
            var before = BoardState.Capture(board); var scoreBefore = board.Score; var events = new List<ResolveEvent>(); board.AreaToolsRemaining--;
            var destroyed = 0; ClearLayer(board, pieceCells, obstacleCells, events, turnId, 1, ref destroyed); GravitySystem.Apply(board, events, turnId);
            return ResolveUntilStable(board, turnId, events, before, scoreBefore, destroyed, 2);
        }

        private static ResolveResult ResolveUntilStable(BoardModel board, int turnId, List<ResolveEvent> events, BoardState before, int scoreBefore, int? destroyedInitial, int nextDepth)
        {
            var depth = nextDepth; var destroyed = destroyedInitial ?? 0; var clearLayers = nextDepth - 1;
            while (true)
            {
                var matches = MatchFinder.Find(board); if (matches.Count == 0) break;
                if (depth > board.Config.MaxChainDepth) { before.Restore(board); throw new InvalidOperationException("Rule error: chain depth limit exceeded."); }
                ClearLayer(board, matches, AdjacentObstacles(board, matches), events, turnId, depth, ref destroyed); GravitySystem.Apply(board, events, turnId); clearLayers++; depth++;
                if (events.Count > board.Config.MaxEvents) { before.Restore(board); throw new InvalidOperationException("Rule error: event queue limit exceeded."); }
            }
            if (board.Config.EnableShuffle && !board.HasLegalMove())
            {
                ShuffleSystem.TryShuffle(board, events, turnId);
            }
            if (events.Count > board.Config.MaxEvents) { before.Restore(board); throw new InvalidOperationException("Rule error: event queue limit exceeded."); }
            return new ResolveResult { IsValid = true, Events = events, Summary = Summary(board, turnId, board.Score - scoreBefore, clearLayers, destroyed) };
        }

        private static HashSet<CellPos> AdjacentObstacles(BoardModel board, IEnumerable<CellPos> cleared)
        {
            var result = new HashSet<CellPos>(); foreach (var pos in cleared) foreach (var neighbor in board.Neighbors(pos)) if (board.Cell(neighbor).Obstacle != null) result.Add(neighbor); return result;
        }

        private static void ClearLayer(BoardModel board, IEnumerable<CellPos> pieceCells, IEnumerable<CellPos> obstacleCells, IList<ResolveEvent> events, int turnId, int depth, ref int destroyed)
        {
            var uniquePieces = pieceCells.Distinct().Where(pos => board.Cell(pos).Piece != null).ToList();
            foreach (var pos in uniquePieces) { var piece = board.Cell(pos).Piece; board.ClearedByColor[piece.Color]++; board.Cell(pos).Piece = null; }
            board.Score += uniquePieces.Count * 10 * depth;
            events.Add(new ResolveEvent(ResolveEventType.Clear, turnId, depth) { AffectedCells = uniquePieces });
            foreach (var pos in obstacleCells.Distinct())
            {
                var obstacle = board.Cell(pos).Obstacle; if (obstacle == null) continue; var isDestroyed = obstacle.Damage(1);
                events.Add(new ResolveEvent(ResolveEventType.Damage, turnId, depth) { TargetCell = pos, RemainingObstacleDurability = obstacle.CurrentDurability, AffectedCells = new List<CellPos> { pos } });
                if (isDestroyed) { board.Cell(pos).Obstacle = null; destroyed++; }
            }
        }

        private static void SwapPieces(BoardModel board, CellPos from, CellPos to)
        {
            var a = board.Cell(from); var b = board.Cell(to); var piece = a.Piece; a.Piece = b.Piece; b.Piece = piece; a.Piece.LogicalPos = from; b.Piece.LogicalPos = to;
        }

        private static ResolveResult Invalid(BoardModel board, int turnId) { return new ResolveResult { IsValid = false, Summary = Summary(board, turnId, 0, 0, 0) }; }
        private static ResolveSummary Summary(BoardModel board, int turnId, int scoreDelta, int chains, int destroyed)
        {
            return new ResolveSummary { TurnId = turnId, ScoreDelta = scoreDelta, ChainCount = chains, RemainingMoves = board.MovesRemaining, RemainingAreaTools = board.AreaToolsRemaining, DestroyedObstacles = destroyed, ClearedByColor = new Dictionary<PieceColor, int>(board.ClearedByColor), FinalSnapshot = board.Snapshot() };
        }
    }
}
