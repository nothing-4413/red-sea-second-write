using System;
using System.Diagnostics;
using RedSea.Match3.Core;
using RedSea.Match3.Core.Rules;

static class Program
{
    private static bool TryFindLegalMove(BoardModel board, out CellPos from, out CellPos to)
    {
        for (var row = 0; row < board.Config.Rows; row++)
            for (var column = 0; column < board.Config.Columns; column++)
            {
                var candidate = new CellPos(row, column);
                foreach (var neighbor in board.Neighbors(candidate))
                    if (SwapValidator.CanSwap(board, candidate, neighbor))
                    {
                        from = candidate;
                        to = neighbor;
                        return true;
                    }
            }
        from = new CellPos();
        to = new CellPos();
        return false;
    }

    public static void Main()
    {
        var config = new LevelConfig { Seed = 202603 };
        var warmup = new BoardModel(config, new SeededRandom(config.Seed));
        if (!TryFindLegalMove(warmup, out var from, out var to)) throw new Exception("FAIL: no legal move for performance case");
        MvpRulePipeline.ResolveSwap(warmup, from, to, 1);

        var expectedSnapshot = string.Empty;
        var expectedScore = 0;
        var stopwatch = Stopwatch.StartNew();
        for (var run = 0; run < 100; run++)
        {
            var board = new BoardModel(config, new SeededRandom(config.Seed));
            var result = MvpRulePipeline.ResolveSwap(board, from, to, 1);
            if (!result.IsValid) throw new Exception("FAIL: fixed performance input became invalid");
            if (run == 0) { expectedSnapshot = board.Snapshot(); expectedScore = board.Score; }
            if (board.Snapshot() != expectedSnapshot || board.Score != expectedScore) throw new Exception("FAIL: fixed replay is not deterministic");
        }
        stopwatch.Stop();
        var averageMilliseconds = stopwatch.Elapsed.TotalMilliseconds / 100.0;
        if (averageMilliseconds >= 50.0) throw new Exception("FAIL: average logic resolution exceeded 50ms: " + averageMilliseconds.ToString("F3"));
        Console.WriteLine("PASS: fixed input is deterministic across 100 runs");
        Console.WriteLine("PASS: average 9x9 logic resolution ms = " + averageMilliseconds.ToString("F3"));
        Console.WriteLine("PERFORMANCE TESTS PASSED");
    }
}
