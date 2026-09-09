using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RedSea.Match3.Architecture;
using RedSea.Match3.Core;
using RedSea.Match3.Core.Rules;
using RedSea.Match3.Flow;
using RedSea.Match3.Testing;

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
        Check("replay starts with snapshot and random cursors", !string.IsNullOrEmpty(resolver.Replay.InitialSnapshot) && resolver.Replay.RecordedRuleVersion == "1.7" && resolver.Replay.InitialRandomIndex > 0 && resolver.Replay.InitialRefillRandomIndex == 0 && resolver.Replay.InitialShuffleRandomIndex == 0 && resolver.Replay.IsCompatible("1.7"));

        var validConfig = new LevelConfig { Rows = 5, Columns = 5, Moves = 10, AreaToolCount = 2, Seed = 71, Obstacles = new List<ObstacleDefinition>() };
        var validResolver = new TurnResolver(Board(new[] { "RBGYP", "GRRBR", "YPGGB", "BRYGP", "GYPRB" }, validConfig));
        var validResult = validResolver.SubmitSwap(new CellPos(1, 3), new CellPos(1, 4), 1);
        Check("resolver submits valid swap to MVP pipeline", validResult.IsValid && validResult.Events.Any(item => item.EventType == ResolveEventType.Swap) && validResult.Events.Any(item => item.EventType == ResolveEventType.Clear));
        var expectedEvents = validResult.Events.ToList();
        var consumedEvents = new List<ResolveEvent>();
        while (validResolver.TryConsume(out var consumed)) consumedEvents.Add(consumed);
        Check("resolver queues complete result in FIFO order", consumedEvents.Count == expectedEvents.Count && consumedEvents.Select(item => item.EventType).SequenceEqual(expectedEvents.Select(item => item.EventType)));
        Check("resolver replay records committed turn", validResolver.Replay.Inputs.Count == 1 && validResolver.Replay.Summaries.Count == 1 && validResolver.Replay.Summaries[0].TurnId == 1);
        var originalSeed = validConfig.Seed;
        Check("replay verifier reproduces committed swap", ReplayVerifier.Verify(validConfig, validResolver.Replay).IsMatch);
        Check("replay verifier does not mutate input config", validConfig.Seed == originalSeed);
        var configCopy = validConfig.Clone();
        configCopy.Colors[0] = PieceColor.Purple;
        if (configCopy.Obstacles.Count > 0) configCopy.Obstacles[0].Durability++;
        Check("level config clone owns mutable collections", validConfig.Colors[0] == PieceColor.Red && (validConfig.Obstacles.Count == 0 || configCopy.Obstacles[0].Durability != validConfig.Obstacles[0].Durability));

        var invalidResolver = new TurnResolver(Board(new[] { "RBGYP", "GBRYR", "YPGGB", "BRYGP", "GYPRB" }, validConfig));
        var invalidBefore = invalidResolver.Board.Snapshot();
        var invalidResult = invalidResolver.SubmitSwap(new CellPos(0, 0), new CellPos(0, 1), 2);
        Check("resolver rejects invalid swap without queue side effect", !invalidResult.IsValid && invalidResolver.Events.Count == 0 && invalidResolver.Replay.Inputs.Count == 0 && invalidResolver.Board.Snapshot() == invalidBefore && invalidResolver.Board.MovesRemaining == 10);

        var toolConfig = new LevelConfig { Rows = 5, Columns = 5, Moves = 10, AreaToolCount = 1, Seed = 72, Obstacles = new List<ObstacleDefinition>() };
        var toolResolver = new TurnResolver(Board(new[] { "RRRBB", "GPGYP", "YGBRG", "PBRYG", "BGYPB" }, toolConfig));
        var movesBeforeTool = toolResolver.Board.MovesRemaining;
        var toolResult = toolResolver.SubmitAreaTool(new CellPos(0, 0), 3);
        Check("resolver submits area tool without consuming move", toolResult.IsValid && toolResolver.Board.MovesRemaining == movesBeforeTool && toolResolver.Board.AreaToolsRemaining == 0);
        Check("resolver queues area tool events and summary", toolResolver.Events.Count == toolResult.Events.Count && toolResolver.Replay.Inputs.Count == 1 && toolResolver.Replay.Inputs[0].Type == ReplayInputType.AreaTool && toolResolver.Replay.Summaries.Count == 1 && toolResult.Summary.RemainingAreaTools == 0);
        var toolEventCount = 0;
        while (toolResolver.TryConsume(out _)) toolEventCount++;
        Check("resolver event queue drains after tool presentation", toolEventCount == toolResult.Events.Count && toolResolver.Events.Count == 0);
        Check("replay verifier reproduces area tool", ReplayVerifier.Verify(toolConfig, toolResolver.Replay).IsMatch);

        var recordingRules = new RecordingRulePipeline();
        var injectedResolver = new TurnResolver(new BoardModel(validConfig, new SeededRandom(validConfig.Seed)), recordingRules);
        injectedResolver.SubmitSwap(new CellPos(0, 0), new CellPos(0, 1), 4);
        injectedResolver.SubmitAreaTool(new CellPos(0, 0), 5);
        Check("resolver depends on injectable rule contract", recordingRules.SwapCalls == 1 && recordingRules.ToolCalls == 1 && injectedResolver.Events.Count == 2);
        var error = injectedResolver.CaptureError(GameErrorType.StateMachine, "test timeout", GameState.Animating, 6);
        Check("error snapshot captures turn state and board context", error.ErrorType == GameErrorType.StateMachine && error.State == GameState.Animating && error.TurnId == 6 && error.Seed == validConfig.Seed && error.BoardSnapshot == injectedResolver.Board.Snapshot());
        var escapedError = injectedResolver.CaptureError(GameErrorType.Presentation, "timeout \"after\"\nrefill", GameState.Animating, 7);
        var serializedError = ErrorSnapshotExporter.Serialize(escapedError);
        using var errorJson = JsonDocument.Parse(serializedError);
        Check("error snapshot export contains versioned diagnostic fields", errorJson.RootElement.GetProperty("schemaVersion").GetInt32() == 1 && errorJson.RootElement.GetProperty("errorType").GetString() == "Presentation" && errorJson.RootElement.GetProperty("turnId").GetInt32() == 7 && errorJson.RootElement.GetProperty("boardSnapshot").GetString() == escapedError.BoardSnapshot);
        Check("error snapshot export escapes diagnostic text", serializedError.Contains("timeout \\\"after\\\"\\nrefill"));
        Check("error snapshot export failure does not throw", !ErrorSnapshotExporter.TryExport(escapedError, null, out var failedPath, out var exportError) && failedPath == null && !string.IsNullOrEmpty(exportError));
        var exportRoot = Path.Combine(Path.GetTempPath(), "redsea-error-snapshot-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var exportTime = new DateTime(2026, 9, 9, 1, 2, 3, 4, DateTimeKind.Utc);
            var firstExport = ErrorSnapshotExporter.Export(escapedError, exportRoot, exportTime);
            var secondExport = ErrorSnapshotExporter.Export(escapedError, exportRoot, exportTime);
            var exportedBytes = File.ReadAllBytes(firstExport);
            Check("error snapshot export writes UTF-8 JSON under bounded directory", File.Exists(firstExport) && Path.GetDirectoryName(firstExport) == Path.Combine(exportRoot, ErrorSnapshotExporter.DirectoryName) && File.ReadAllText(firstExport) == serializedError && (exportedBytes.Length < 3 || exportedBytes[0] != 0xef || exportedBytes[1] != 0xbb || exportedBytes[2] != 0xbf));
            Check("error snapshot export never overwrites same-turn diagnostics", firstExport != secondExport && File.Exists(secondExport));
        }
        finally
        {
            if (Directory.Exists(exportRoot)) Directory.Delete(exportRoot, true);
        }
        var invalidLimitConfig = new LevelConfig { MaxInitialGenerationAttempts = 0 };
        try { invalidLimitConfig.Validate(); Check("invalid initial generation limit rejected", false); } catch (InvalidOperationException) { Check("invalid initial generation limit rejected", true); }

        var inputBoard = Board(new[] { "RBGYP", "GRRBR", "YPGGB", "BRYGP", "GYPRB" }, validConfig);
        var inputResolver = new TurnResolver(inputBoard);
        var inputState = new TurnStateMachine();
        var input = new InputController(inputBoard, inputResolver, inputState);
        var selection = input.Click(new CellPos(1, 3));
        var committed = input.Click(new CellPos(1, 4));
        Check("input controller owns selection and submits through resolver", selection.Type == BoardInputResultType.Selected && committed.Type == BoardInputResultType.SwapCommitted && committed.Resolution.IsValid && input.TurnId == 1 && !input.Selected.HasValue && inputState.State == GameState.Animating);

        foreach (var terminalState in new[] { GameState.Win, GameState.Lose })
        {
            var terminalBoard = Board(new[] { "RBGYP", "GRRBR", "YPGGB", "BRYGP", "GYPRB" }, validConfig);
            var terminalResolver = new TurnResolver(terminalBoard);
            var terminalMachine = new TurnStateMachine();
            var terminalInput = new InputController(terminalBoard, terminalResolver, terminalMachine);
            terminalMachine.Finish(terminalState);
            var terminalSnapshot = terminalBoard.Snapshot();
            var terminalMoves = terminalBoard.MovesRemaining;
            var terminalRandomIndex = terminalBoard.RefillRandom.Index;
            var allLocked = true;
            for (var click = 0; click < 20; click++) allLocked &= terminalInput.Click(new CellPos(click % 5, (click * 3) % 5)).Type == BoardInputResultType.Locked;
            Check(terminalState.ToString().ToLowerInvariant() + " ignores 20 board clicks without side effects", allLocked && terminalBoard.Snapshot() == terminalSnapshot && terminalBoard.MovesRemaining == terminalMoves && terminalBoard.RefillRandom.Index == terminalRandomIndex && terminalResolver.Events.Count == 0 && terminalResolver.Replay.Inputs.Count == 0 && terminalInput.TurnId == 0);
        }
        var fixedReport = FixedTestScenario.Run();
        Check("test scene fixed scenario runs through production input and resolver", fixedReport.Passed && fixedReport.EventCount > 0 && !string.IsNullOrEmpty(fixedReport.FinalSnapshot));
        Console.WriteLine($"ARCHITECTURE CONTRACT TESTS PASSED: {passed}");
    }
}
