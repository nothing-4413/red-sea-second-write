using System;
using System.Collections.Generic;
using RedSea.Match3.Core;

namespace RedSea.Match3.Core.Diagnostics
{
    public enum BoardIntegrityIssueCode
    {
        DuplicatePieceId,
        PiecePositionMismatch,
        NegativeObstacleDurability,
        UnexpectedEmptyCell
    }

    public sealed class BoardIntegrityIssue
    {
        public BoardIntegrityIssueCode Code { get; private set; }
        public string Message { get; private set; }
        public CellPos Position { get; private set; }
        public int PieceId { get; private set; }

        public BoardIntegrityIssue(BoardIntegrityIssueCode code, string message, CellPos position, int pieceId = 0)
        {
            Code = code;
            Message = message;
            Position = position;
            PieceId = pieceId;
        }
    }

    public sealed class BoardIntegrityReport
    {
        public IReadOnlyList<BoardIntegrityIssue> Issues { get; private set; }
        public bool IsValid { get { return Issues.Count == 0; } }

        public BoardIntegrityReport(IReadOnlyList<BoardIntegrityIssue> issues)
        {
            Issues = issues ?? throw new ArgumentNullException(nameof(issues));
        }

        public string Describe()
        {
            if (IsValid) return "Board integrity is valid.";
            var parts = new List<string>();
            foreach (var issue in Issues) parts.Add(issue.Code + " at " + issue.Position + ": " + issue.Message);
            return string.Join("; ", parts);
        }
    }

    public static class BoardIntegrityValidator
    {
        // Validation runs at command boundaries, when transient clear/fall gaps are not allowed.
        public static BoardIntegrityReport Validate(BoardModel board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            var issues = new List<BoardIntegrityIssue>();
            var piecePositions = new Dictionary<int, CellPos>();
            for (var row = 0; row < board.Config.Rows; row++)
            {
                for (var column = 0; column < board.Config.Columns; column++)
                {
                    var position = new CellPos(row, column);
                    var cell = board.Cells[row, column];
                    if (cell == null) continue;

                    if (cell.Obstacle != null && cell.Obstacle.CurrentDurability < 0)
                    {
                        issues.Add(new BoardIntegrityIssue(
                            BoardIntegrityIssueCode.NegativeObstacleDurability,
                            "Obstacle durability cannot be negative.", position));
                    }

                    if (cell.Piece == null)
                    {
                        if (cell.Obstacle == null)
                        {
                            issues.Add(new BoardIntegrityIssue(
                                BoardIntegrityIssueCode.UnexpectedEmptyCell,
                                "A traversable command-boundary cell must contain a piece.", position));
                        }
                        continue;
                    }

                    var piece = cell.Piece;
                    if (piece.LogicalPos != position)
                    {
                        issues.Add(new BoardIntegrityIssue(
                            BoardIntegrityIssueCode.PiecePositionMismatch,
                            "Piece.LogicalPos does not match its board cell.", position, piece.PieceId));
                    }

                    if (piecePositions.TryGetValue(piece.PieceId, out var previousPosition))
                    {
                        issues.Add(new BoardIntegrityIssue(
                            BoardIntegrityIssueCode.DuplicatePieceId,
                            "PieceId is already used at " + previousPosition + ".", position, piece.PieceId));
                    }
                    else
                    {
                        piecePositions.Add(piece.PieceId, position);
                    }
                }
            }
            return new BoardIntegrityReport(issues);
        }
    }
}
