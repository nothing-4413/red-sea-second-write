using RedSea.Match3.Core;
using RedSea.Match3.Core.Rules;

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
        var result = MvpRulePipeline.ResolveSwap(board, new CellPos(1, 3), new CellPos(1, 4), 1);
        Check("valid swap commits", result.IsValid && board.MovesRemaining == 9 && result.Events.Any(e => e.EventType == ResolveEventType.Clear));
        board = Board(new[] { "RBGYP", "GBRYR", "YPGGB", "BRYGP", "GYPRB" }, new LevelConfig { Rows = 5, Columns = 5, Moves = 10, Obstacles = new List<ObstacleDefinition>() });
        var before = board.Snapshot(); var randomIndex = board.RefillRandom.Index; result = MvpRulePipeline.ResolveSwap(board, new CellPos(0, 0), new CellPos(0, 1), 2);
        Check("invalid swap rolls back", !result.IsValid && board.Snapshot() == before && board.MovesRemaining == 10 && board.RefillRandom.Index == randomIndex);
        board = Board(new[] { "RBGYP", "GBRYR", "YPGGB", "BRYGP", "GYPRB" }, new LevelConfig { Rows = 5, Columns = 5, Moves = 10, Obstacles = new List<ObstacleDefinition> { new ObstacleDefinition(2, 2, 1) } });
        Check("obstacle blocks swap", !SwapValidator.CanSwap(board, new CellPos(2, 2), new CellPos(2, 3)));
        board = Board(new[] { "RRRBB", "GPGYP", "YGBRG", "PBRYG", "BGYPB" }, new LevelConfig { Rows = 5, Columns = 5, Moves = 4, Obstacles = new List<ObstacleDefinition>() });
        var scoreBefore = board.Score; result = MvpRulePipeline.ResolveAreaTool(board, new CellPos(0, 0), 3); Check("area tool clips and keeps moves", result.IsValid && board.MovesRemaining == 4); Check("area tool consumes model inventory", board.AreaToolsRemaining == 1); Check("summary score delta matches model", result.Summary.ScoreDelta == board.Score - scoreBefore); Check("area resolution ends stable", MatchFinder.Find(board).Count == 0);
        var segmentConfig = new LevelConfig { Rows = 5, Columns = 5, Obstacles = new List<ObstacleDefinition> { new ObstacleDefinition(2, 2, 1) } }; var segmentBoard = Board(new[] { "RBGYP", "GBRYR", "YPGGB", "BRYGP", "GYPRB" }, segmentConfig); var upperId = segmentBoard.Cells[0, 2].Piece.PieceId; segmentBoard.Cells[3, 2].Piece = null; segmentBoard.Cells[4, 2].Piece = null; var segmentEvents = new List<ResolveEvent>(); GravitySystem.Apply(segmentBoard, segmentEvents, 9); Check("obstacle remains a gravity boundary", segmentBoard.Cells[2, 2].Obstacle != null && segmentBoard.Cells[0, 2].Piece.PieceId == upperId);

        segmentBoard.AreaToolsRemaining = 0; var invalidToolResult = MvpRulePipeline.ResolveAreaTool(segmentBoard, new CellPos(0, 0), 10); Check("invalid tool has no inventory side effect", !invalidToolResult.IsValid && segmentBoard.AreaToolsRemaining == 0);
        Console.WriteLine($"ALL TESTS PASSED: {passed}");
    }
}
