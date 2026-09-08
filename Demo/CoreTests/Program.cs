using RedSea.Match3.Core;

static class Test
{
    static int passed;
    static void Check(string name, bool condition) { if (!condition) throw new Exception("FAIL: " + name); passed++; Console.WriteLine("PASS: " + name); }
    static BoardModel Board(string[] rows, LevelConfig config = null)
    {
        config ??= new LevelConfig { Rows = rows.Length, Columns = rows[0].Length, Obstacles = new List<ObstacleDefinition>() };
        var board = new BoardModel(config, new SeededRandom(7));
        for (var r = 0; r < rows.Length; r++) for (var c = 0; c < rows[r].Length; c++) board.Cells[r, c].Piece = rows[r][c] == '.' ? null : board.CreatePiece(Color(rows[r][c]), new CellPos(r, c));
        return board;
    }
    static PieceColor Color(char c) => c == 'R' ? PieceColor.Red : c == 'B' ? PieceColor.Blue : c == 'G' ? PieceColor.Green : c == 'Y' ? PieceColor.Yellow : PieceColor.Purple;
    static void Main()
    {
        var generatedA = new BoardModel(new LevelConfig(), new SeededRandom(123)); var generatedB = new BoardModel(new LevelConfig(), new SeededRandom(123)); Check("initial board stable", MatchFinder.Find(generatedA).Count == 0 && generatedA.HasLegalMove()); Check("initial seed deterministic", generatedA.Snapshot() == generatedB.Snapshot());
        var board = Board(new[] { "RRRBB", "GPGYP", "YGBRG", "PBRYG", "BGYPB" });
        Check("horizontal match", MatchFinder.Find(board).Count == 3);
        board = Board(new[] { "RBGYP", "GRRBR", "YPGGB", "BRYGP", "GYPRB" }, new LevelConfig { Rows = 5, Columns = 5, Moves = 10, Obstacles = new List<ObstacleDefinition>() });
        var result = ResolveSystem.Swap(board, new CellPos(1, 3), new CellPos(1, 4), 1);
        Check("valid swap commits", result.IsValid && board.MovesRemaining == 9 && result.Events.Any(e => e.EventType == ResolveEventType.Clear));
        board = Board(new[] { "RBGYP", "GBRYR", "YPGGB", "BRYGP", "GYPRB" }, new LevelConfig { Rows = 5, Columns = 5, Moves = 10, Obstacles = new List<ObstacleDefinition>() });
        var before = board.Snapshot(); var randomIndex = board.RefillRandom.Index; result = ResolveSystem.Swap(board, new CellPos(0, 0), new CellPos(0, 1), 2);
        Check("invalid swap rolls back", !result.IsValid && board.Snapshot() == before && board.MovesRemaining == 10 && board.RefillRandom.Index == randomIndex);
        board = Board(new[] { "RBGYP", "GBRYR", "YPGGB", "BRYGP", "GYPRB" }, new LevelConfig { Rows = 5, Columns = 5, Moves = 10, Obstacles = new List<ObstacleDefinition> { new ObstacleDefinition(2, 2, 1) } });
        Check("obstacle blocks swap", !SwapValidator.CanSwap(board, new CellPos(2, 2), new CellPos(2, 3)));
        board = Board(new[] { "RRRBB", "GPGYP", "YGBRG", "PBRYG", "BGYPB" }, new LevelConfig { Rows = 5, Columns = 5, Moves = 4, Obstacles = new List<ObstacleDefinition>() });
        result = ResolveSystem.AreaTool(board, new CellPos(0, 0), 3); Check("area tool clips and keeps moves", result.IsValid && board.MovesRemaining == 4);
        Console.WriteLine($"ALL TESTS PASSED: {passed}");
    }
}
