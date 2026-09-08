using RedSea.Match3.Core;
using RedSea.Match3.Architecture;
using RedSea.Match3.Core.Rules;

static void Check(string name, bool value) { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
static BoardModel Board(string[] rows, LevelConfig config)
{
    var board = new BoardModel(config, new SeededRandom(config.Seed));
    for (var row = 0; row < rows.Length; row++)
        for (var column = 0; column < rows[row].Length; column++)
            board.Cells[row, column].Piece = rows[row][column] == '.' || rows[row][column] == 'X'
                ? null
                : board.CreatePiece(Color(rows[row][column]), new CellPos(row, column));
    return board;
}
static PieceColor Color(char color) => color == 'R' ? PieceColor.Red : color == 'B' ? PieceColor.Blue : color == 'G' ? PieceColor.Green : color == 'Y' ? PieceColor.Yellow : PieceColor.Purple;
var queue = new EventQueue(3);
queue.EnqueueRange(new[] { new ResolveEvent(ResolveEventType.Swap, 1), new ResolveEvent(ResolveEventType.Clear, 1), new ResolveEvent(ResolveEventType.Summary, 1) });
Check("event queue preserves FIFO", queue.TryDequeue(out var first) && first.EventType == ResolveEventType.Swap && queue.Count == 2);
Check("event queue drains", queue.TryDequeue(out _) && queue.TryDequeue(out _) && !queue.TryDequeue(out _));
var config = new LevelConfig { Rows = 5, Columns = 5, Moves = 3, GoalCount = 1, Obstacles = new List<ObstacleDefinition>() };
var board = new BoardModel(config, new SeededRandom(11));
var resolver = new TurnResolver(board);
Check("resolver owns event queue", resolver.Events.Count == 0 && resolver.Replay.Seed == 11 || resolver.Replay.Seed == config.Seed);
Check("goal system reads model", resolver.Goals.IsFailed(board) == false);
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
Console.WriteLine("ARCHITECTURE CONTRACT TESTS PASSED");
