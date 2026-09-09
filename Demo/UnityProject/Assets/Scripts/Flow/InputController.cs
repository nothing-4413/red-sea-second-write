using System;
using RedSea.Match3.Architecture;
using RedSea.Match3.Core;

namespace RedSea.Match3.Flow
{
    public enum BoardInputResultType
    {
        Ignored,
        Locked,
        ObstacleBlocked,
        Selected,
        Reselected,
        Cancelled,
        InvalidSwap,
        SwapCommitted,
        ToolUnavailable,
        InvalidToolTarget,
        ToolCommitted
    }

    public sealed class BoardInputResult
    {
        public BoardInputResultType Type { get; private set; }
        public CellPos? Selection { get; private set; }
        public ResolveResult Resolution { get; private set; }

        public BoardInputResult(BoardInputResultType type, CellPos? selection = null, ResolveResult resolution = null)
        {
            Type = type;
            Selection = selection;
            Resolution = resolution;
        }
    }

    public sealed class InputController
    {
        private readonly BoardModel board;
        private readonly TurnResolver resolver;
        private readonly TurnStateMachine stateMachine;

        public CellPos? Selected { get; private set; }
        public int TurnId { get; private set; }

        public InputController(BoardModel board, TurnResolver resolver, TurnStateMachine stateMachine)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        public BoardInputResult Click(CellPos position)
        {
            if (stateMachine.InputLocked) return Result(BoardInputResultType.Locked);
            if (!board.Config.IsInBounds(position)) return Result(BoardInputResultType.Ignored);
            if (board.Cell(position).Obstacle != null) return Result(BoardInputResultType.ObstacleBlocked);
            if (board.Cell(position).Piece == null) return Result(BoardInputResultType.Ignored);
            if (!Selected.HasValue)
            {
                Selected = position;
                stateMachine.Select();
                return Result(BoardInputResultType.Selected);
            }
            if (Selected.Value == position)
            {
                Selected = null;
                stateMachine.ReturnToIdle();
                return Result(BoardInputResultType.Cancelled);
            }
            if (!SwapValidator.IsAdjacent(Selected.Value, position))
            {
                Selected = position;
                return Result(BoardInputResultType.Reselected);
            }
            return SubmitSwap(Selected.Value, position);
        }

        public BoardInputResult SubmitSwap(CellPos from, CellPos to)
        {
            if (stateMachine.InputLocked) return Result(BoardInputResultType.Locked);
            if (stateMachine.State == GameState.Idle) stateMachine.Select();
            stateMachine.BeginResolve();
            var resolution = resolver.SubmitSwap(from, to, ++TurnId);
            Selected = null;
            if (!resolution.IsValid)
            {
                stateMachine.ReturnToIdle();
                return Result(BoardInputResultType.InvalidSwap, resolution);
            }
            stateMachine.BeginAnimation();
            return Result(BoardInputResultType.SwapCommitted, resolution);
        }

        public BoardInputResult UseAreaTool(CellPos center)
        {
            if (stateMachine.InputLocked) return Result(BoardInputResultType.Locked);
            if (board.AreaToolsRemaining <= 0) return Result(BoardInputResultType.ToolUnavailable);
            if (!board.Config.IsInBounds(center)) return Result(BoardInputResultType.InvalidToolTarget);
            stateMachine.Enter(GameState.Resolving);
            var resolution = resolver.SubmitAreaTool(center, ++TurnId);
            if (!resolution.IsValid)
            {
                stateMachine.ReturnToIdle();
                return Result(BoardInputResultType.InvalidToolTarget, resolution);
            }
            Selected = null;
            stateMachine.BeginAnimation();
            return Result(BoardInputResultType.ToolCommitted, resolution);
        }

        private BoardInputResult Result(BoardInputResultType type, ResolveResult resolution = null)
        {
            return new BoardInputResult(type, Selected, resolution);
        }
    }
}
