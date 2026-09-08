using System;
using System.Collections.Generic;
using System.Linq;
using RedSea.Match3.Architecture;
using RedSea.Match3.Core;
using RedSea.Match3.Core.Rules;

sealed class RecordingRulePipeline : IRulePipeline
{
    public int SwapCalls;
    public int ToolCalls;
    public ResolveResult ResolveSwap(BoardModel board, CellPos from, CellPos to, int turnId)
    {
        SwapCalls++;
        return Result(turnId, ResolveEventType.Swap);
    }
    public ResolveResult ResolveAreaTool(BoardModel board, CellPos center, int turnId)
    {
        ToolCalls++;
        return Result(turnId, ResolveEventType.Clear);
    }
    private static ResolveResult Result(int turnId, ResolveEventType eventType)
    {
        return new ResolveResult
        {
            IsValid = true,
            Events = new List<ResolveEvent> { new ResolveEvent(eventType, turnId) },
            Summary = new ResolveSummary { TurnId = turnId, RemainingMoves = 10, RemainingAreaTools = 1, FinalSnapshot = "stub" }
        };
    }
}

static class Program
{
    private static int passed;

    private static void Check(string name, bool condition)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        passed++;
        Console.WriteLine("PASS: " + name);
    }

    private static BoardModel Board(string[] rows, LevelConfig config)
    {
        var board = new BoardModel(config, new SeededRandom(config.Seed));
        for (var row = 0; row < rows.Length; row++)
            for (var column = 0; column < rows[row].Length; column++)
                board.Cells[row, column].Piece = rows[row][column] == '.' || rows[row][column] == 'X'
                    ? null
                    : board.CreatePiece(Color(rows[row][column]), new CellPos(row, column));
        return board;
    }

    private static PieceColor Color(char color)
    {
        return color == 'R' ? PieceColor.Red : color == 'B' ? PieceColor.Blue : color == 'G' ? PieceColor.Green : color == 'Y' ? PieceColor.Yellow : PieceColor.Purple;
    }

    public static void Main()
    {
        var queue = new EventQueue(3);
        queue.EnqueueRange(new[] { new ResolveEvent(ResolveEventType.Swap, 1), new ResolveEvent(ResolveEventType.Clear, 1), new ResolveEvent(ResolveEventType.Summary, 1) });
        Check("event queue preserves FIFO", queue.TryDequeue(out var first) && first.EventType == ResolveEventType.Swap && queue.Count == 2);
        Check("event queue drains", queue.TryDequeue(out _) && queue.TryDequeue(out _) && !queue.TryDequeue(out _));

        var config = new LevelConfig { Rows = 5, Columns = 5, Moves = 3, GoalCount = 1, Obstacles = new List<ObstacleDefinition>() };
        var resolver = new TurnResolver(new BoardModel(config, new SeededRandom(11)));
        Check("resolver owns event queue", resolver.Events.Count == 0 && (resolver.Replay.Seed == 11 || resolver.Replay.Seed == config.Seed));
        Check("goal system reads model", resolver.Goals.IsFailed(resolver.Board) == false);
        Check("replay starts with snapshot", !string.IsNullOrEmpty(resolver.Replay.InitialSnapshot) && ReplayRecord.RuleVersion == "1.7");

        var validConfig = new LevelConfig { Rows = 5, Columns = 5, Moves = 10, AreaToolCount = 2, Seed = 71, Obstacles = new List<ObstacleDefinition>() };
        var validResolver = new TurnResolver(Board(new[] { "RBGYP", "GRRBR", "YPGGB", "BRYGP", "GYPRB" }, validConfig));
        var validResult = validResolver.SubmitSwap(new CellPos(1, 3), new CellPos(1, 4), 1);
        Check("resolver submits valid swap to MVP pipeline", validResult.IsValid && validResult.Events.Any(item => item.EventType == ResolveEventType.Swap) && validResult.Events.Any(item => item.EventType == ResolveEventType.Clear));
        var expectedEvents = validResult.Events.ToList();
        var consumedEvents = new List<ResolveEvent>();
        while (validResolver.TryConsume(out var consumed)) consumedEvents.Add(consumed);
        Check("resolver queues complete result in FIFO order", consumedEvents.Count == expectedEvents.Count && consumedEvents.Select(item => item.EventType).SequenceEqual(expectedEvents.Select(item => item.EventType)));
        Check("resolver replay records committed turn", validResolver.Replay.Inputs.Count == 1 && validResolver.Replay.Summaries.Count == 1 && validResolver.Replay.Summaries[0].TurnId == 1);

        var invalidResolver = new TurnResolver(Board(new[] { "RBGYP", "GBRYR", "YPGGB", "BRYGP", "GYPRB" }, validConfig));
        var invalidBefore = invalidResolver.Board.Snapshot();
        var invalidResult = invalidResolver.SubmitSwap(new CellPos(0, 0), new CellPos(0, 1), 2);
        Check("resolver rejects invalid swap without queue side effect", !invalidResult.IsValid && invalidResolver.Events.Count == 0 && invalidResolver.Replay.Inputs.Count == 0 && invalidResolver.Board.Snapshot() == invalidBefore && invalidResolver.Board.MovesRemaining == 10);

        var toolConfig = new LevelConfig { Rows = 5, Columns = 5, Moves = 10, AreaToolCount = 1, Seed = 72, Obstacles = new List<ObstacleDefinition>() };
        var toolResolver = new TurnResolver(Board(new[] { "RRRBB", "GPGYP", "YGBRG", "PBRYG", "BGYPB" }, toolConfig));
        var movesBeforeTool = toolResolver.Board.MovesRemaining;
        var toolResult = toolResolver.SubmitAreaTool(new CellPos(0, 0), 3);
        Check("resolver submits area tool without consuming move", toolResult.IsValid && toolResolver.Board.MovesRemaining == movesBeforeTool && toolResolver.Board.AreaToolsRemaining == 0);
        Check("resolver queues area tool events and summary", toolResolver.Events.Count == toolResult.Events.Count && toolResolver.Replay.Summaries.Count == 1 && toolResult.Summary.RemainingAreaTools == 0);
        var toolEventCount = 0;
        while (toolResolver.TryConsume(out _)) toolEventCount++;
        Check("resolver event queue drains after tool presentation", toolEventCount == toolResult.Events.Count && toolResolver.Events.Count == 0);

        var recordingRules = new RecordingRulePipeline();
        var injectedResolver = new TurnResolver(new BoardModel(validConfig, new SeededRandom(validConfig.Seed)), recordingRules);
        injectedResolver.SubmitSwap(new CellPos(0, 0), new CellPos(0, 1), 4);
        injectedResolver.SubmitAreaTool(new CellPos(0, 0), 5);
        Check("resolver depends on injectable rule contract", recordingRules.SwapCalls == 1 && recordingRules.ToolCalls == 1 && injectedResolver.Events.Count == 2);
        Console.WriteLine($"ARCHITECTURE CONTRACT TESTS PASSED: {passed}");
    }
}
