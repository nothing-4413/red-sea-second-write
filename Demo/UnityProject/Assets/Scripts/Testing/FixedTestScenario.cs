using System;
using System.Collections.Generic;
using RedSea.Match3.Architecture;
using RedSea.Match3.Core;
using RedSea.Match3.Flow;

namespace RedSea.Match3.Testing
{
    public sealed class FixedTestReport
    {
        public bool Passed { get; internal set; }
        public string Message { get; internal set; }
        public int EventCount { get; internal set; }
        public string FinalSnapshot { get; internal set; }
    }

    public static class FixedTestScenario
    {
        private static readonly string[] Rows = { "RBGYP", "GRRBR", "YPGGB", "BRYGP", "GYPRB" };

        public static FixedTestReport Run()
        {
            try
            {
                var config = new LevelConfig { Rows = 5, Columns = 5, Moves = 10, Seed = 71, Obstacles = new List<ObstacleDefinition>() };
                var board = CreateBoard(config);
                var resolver = new TurnResolver(board);
                var machine = new TurnStateMachine();
                var input = new InputController(board, resolver, machine);
                var selected = input.Click(new CellPos(1, 3));
                var committed = input.Click(new CellPos(1, 4));
                if (selected.Type != BoardInputResultType.Selected || committed.Type != BoardInputResultType.SwapCommitted) return Fail("fixed swap was not submitted through InputController", board);
                if (machine.State != GameState.Animating || resolver.Events.Count == 0) return Fail("fixed swap did not enter animation with events", board);
                var eventCount = 0;
                while (resolver.TryConsume(out _)) eventCount++;
                machine.BeginRefill(); machine.CheckChain(); machine.ReturnToIdle();
                if (machine.State != GameState.Idle || MatchFinder.Find(board).Count != 0) return Fail("fixed swap did not finish on a stable board", board, eventCount);
                var terminalSnapshot = board.Snapshot();
                machine.Finish(GameState.Win);
                for (var click = 0; click < 20; click++) if (input.Click(new CellPos(click % 5, (click * 3) % 5)).Type != BoardInputResultType.Locked) return Fail("terminal input was not locked", board, eventCount);
                if (board.Snapshot() != terminalSnapshot || board.MovesRemaining != 9 || resolver.Replay.Inputs.Count != 1 || resolver.Events.Count != 0) return Fail("terminal clicks changed logical state", board, eventCount);
                return new FixedTestReport { Passed = true, Message = "Fixed test scenario passed.", EventCount = eventCount, FinalSnapshot = board.Snapshot() };
            }
            catch (Exception exception)
            {
                return new FixedTestReport { Passed = false, Message = exception.Message };
            }
        }

        private static BoardModel CreateBoard(LevelConfig config)
        {
            var board = new BoardModel(config, new SeededRandom(config.Seed));
            for (var row = 0; row < Rows.Length; row++) for (var column = 0; column < Rows[row].Length; column++) board.Cells[row, column].Piece = board.CreatePiece(Color(Rows[row][column]), new CellPos(row, column));
            return board;
        }

        private static PieceColor Color(char color)
        {
            return color == 'R' ? PieceColor.Red : color == 'B' ? PieceColor.Blue : color == 'G' ? PieceColor.Green : color == 'Y' ? PieceColor.Yellow : PieceColor.Purple;
        }

        private static FixedTestReport Fail(string message, BoardModel board, int eventCount = 0)
        {
            return new FixedTestReport { Passed = false, Message = message, EventCount = eventCount, FinalSnapshot = board.Snapshot() };
        }
    }
}
