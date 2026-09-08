using RedSea.Match3.Core;
using RedSea.Match3.Architecture;

static void Check(string name, bool value) { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
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
Console.WriteLine("ARCHITECTURE CONTRACT TESTS PASSED");
