using System;
using System.Collections.Generic;
using System.Linq;

namespace RedSea.Match3.Core
{
    public enum PieceColor { Red, Blue, Green, Yellow, Purple }
    public enum SpecialPieceType { None, Rocket, Bomb, FlyingBomb, ColorBomb }
    public enum ObstacleType { Crate }
    public enum GameState { Idle, Selecting, Resolving, Animating, Refilling, CheckingChain, Paused, Win, Lose }
    public enum ResolveEventType { Swap, Clear, Damage, Fall, Refill, Shuffle, Summary }
    public enum GoalType { CollectColor, DestroyObstacle, Score, Chain }

    public struct CellPos : IEquatable<CellPos>
    {
        public int Row; public int Column;
        public CellPos(int row, int column) { Row = row; Column = column; }
        public bool Equals(CellPos other) { return Row == other.Row && Column == other.Column; }
        public override bool Equals(object obj) { return obj is CellPos && Equals((CellPos)obj); }
        public override int GetHashCode() { return (Row * 397) ^ Column; }
        public override string ToString() { return "(" + Row + "," + Column + ")"; }
        public static bool operator ==(CellPos a, CellPos b) { return a.Equals(b); }
        public static bool operator !=(CellPos a, CellPos b) { return !a.Equals(b); }
    }

    [Serializable]
    public class ObstacleDefinition
    {
        public int Row; public int Column; public int Durability;
        public ObstacleDefinition() { }
        public ObstacleDefinition(int row, int column, int durability) { Row = row; Column = column; Durability = durability; }
    }

    public class LevelConfig
    {
        public int Rows = 9, Columns = 9, Moves = 24, AreaToolCount = 2, Seed = 202603;
        public PieceColor[] Colors = { PieceColor.Red, PieceColor.Blue, PieceColor.Green, PieceColor.Yellow, PieceColor.Purple };
        public PieceColor GoalColor = PieceColor.Red; public int GoalCount = 18;
        public int MaxChainDepth = 20, MaxEvents = 500, MaxShuffleAttempts = 8, MaxInitialGenerationAttempts = 100;
        public float EventDurationSeconds = 0.06f, MaxTurnWaitSeconds = 10f;
        public bool EnableRocket, EnableBomb, EnableFlyingBomb, EnableColorBomb, EnableSpecialCombo, EnableShuffle = true;
        public List<ObstacleDefinition> Obstacles = new List<ObstacleDefinition> { new ObstacleDefinition(3, 4, 2), new ObstacleDefinition(5, 4, 1) };
        public bool IsInBounds(CellPos pos) { return pos.Row >= 0 && pos.Row < Rows && pos.Column >= 0 && pos.Column < Columns; }
        public LevelConfig Clone()
        {
            return new LevelConfig
            {
                Rows = Rows, Columns = Columns, Moves = Moves, AreaToolCount = AreaToolCount, Seed = Seed,
                Colors = Colors == null ? null : Colors.ToArray(),
                GoalColor = GoalColor, GoalCount = GoalCount,
                MaxChainDepth = MaxChainDepth, MaxEvents = MaxEvents, MaxShuffleAttempts = MaxShuffleAttempts, MaxInitialGenerationAttempts = MaxInitialGenerationAttempts,
                EventDurationSeconds = EventDurationSeconds, MaxTurnWaitSeconds = MaxTurnWaitSeconds,
                EnableRocket = EnableRocket, EnableBomb = EnableBomb, EnableFlyingBomb = EnableFlyingBomb, EnableColorBomb = EnableColorBomb, EnableSpecialCombo = EnableSpecialCombo, EnableShuffle = EnableShuffle,
                Obstacles = Obstacles == null ? null : Obstacles.Select(item => new ObstacleDefinition(item.Row, item.Column, item.Durability)).ToList()
            };
        }
        public void Validate()
        {
            if (Rows <= 0 || Columns <= 0 || Colors == null || Colors.Length < 3 || Moves < 0 || MaxChainDepth <= 0 || MaxEvents <= 0 || MaxShuffleAttempts <= 0 || MaxInitialGenerationAttempts <= 0 || EventDurationSeconds < 0 || MaxTurnWaitSeconds <= 0) throw new InvalidOperationException("Invalid LevelConfig dimensions, colors, moves or safety limits.");
            var seen = new HashSet<CellPos>(); foreach (var item in Obstacles) { var pos = new CellPos(item.Row, item.Column); if (!IsInBounds(pos) || item.Durability <= 0 || !seen.Add(pos)) throw new InvalidOperationException("Invalid obstacle definition."); }
        }
    }

    public class SeededRandom
    {
        public uint State { get; private set; } public int Index { get; private set; }
        public SeededRandom(int seed) { State = unchecked((uint)seed); }
        public int Next(int maxExclusive) { if (maxExclusive <= 0) throw new ArgumentOutOfRangeException("maxExclusive"); State = unchecked(1664525u * State + 1013904223u); Index++; return (int)(State % (uint)maxExclusive); }
        public void Restore(uint state, int index) { State = state; Index = index; }
    }

    public class Piece
    {
        public int PieceId; public PieceColor Color; public SpecialPieceType SpecialType; public CellPos LogicalPos;
        public Piece(int id, PieceColor color, CellPos pos) { PieceId = id; Color = color; LogicalPos = pos; SpecialType = SpecialPieceType.None; }
        public Piece Clone() { return new Piece(PieceId, Color, LogicalPos) { SpecialType = SpecialType }; }
    }

    public class Obstacle
    {
        public ObstacleType Type; public int CurrentDurability; public int MaxDurability; public bool CanSwap; public bool CanFallThrough;
        public Obstacle(ObstacleType type, int durability) { Type = type; CurrentDurability = durability; MaxDurability = durability; }
        public bool Damage(int amount) { CurrentDurability = Math.Max(0, CurrentDurability - amount); return CurrentDurability == 0; }
        public Obstacle Clone() { return new Obstacle(Type, CurrentDurability) { MaxDurability = MaxDurability, CanSwap = CanSwap, CanFallThrough = CanFallThrough }; }
    }

    public class BoardCell
    {
        public CellPos Pos; public Piece Piece; public Obstacle Obstacle;
        public BoardCell(CellPos pos) { Pos = pos; }
    }

    public class BoardModel
    {
        public LevelConfig Config { get; private set; } public BoardCell[,] Cells { get; private set; } public SeededRandom InitialRandom { get; private set; } public SeededRandom RefillRandom { get; private set; } public SeededRandom ShuffleRandom { get; private set; }
        public int MovesRemaining; public int AreaToolsRemaining; public int Score; public int NextPieceId = 1;
        public Dictionary<PieceColor, int> ClearedByColor = Enum.GetValues(typeof(PieceColor)).Cast<PieceColor>().ToDictionary(c => c, c => 0);
        public BoardModel(LevelConfig config, SeededRandom random = null)
        {
            Config = config; Config.Validate(); InitialRandom = random ?? new SeededRandom(config.Seed); RefillRandom = new SeededRandom(config.Seed ^ 0x5A17); ShuffleRandom = new SeededRandom(config.Seed ^ 0x3C41); MovesRemaining = config.Moves; AreaToolsRemaining = config.AreaToolCount; Cells = new BoardCell[config.Rows, config.Columns];
            for (var r = 0; r < config.Rows; r++) for (var c = 0; c < config.Columns; c++) Cells[r, c] = new BoardCell(new CellPos(r, c));
            foreach (var item in config.Obstacles) Cells[item.Row, item.Column].Obstacle = new Obstacle(ObstacleType.Crate, item.Durability); FillInitial();
        }
        public BoardCell Cell(CellPos pos) { return Config.IsInBounds(pos) ? Cells[pos.Row, pos.Column] : null; }
        public IEnumerable<CellPos> Neighbors(CellPos pos) { return new[] { new CellPos(pos.Row - 1, pos.Column), new CellPos(pos.Row + 1, pos.Column), new CellPos(pos.Row, pos.Column - 1), new CellPos(pos.Row, pos.Column + 1) }.Where(Config.IsInBounds); }
        public Piece CreatePiece(PieceColor color, CellPos pos) { return new Piece(NextPieceId++, color, pos); }
        public void FillInitial() { for (var attempt = 0; attempt < Config.MaxInitialGenerationAttempts; attempt++) { for (var r = 0; r < Config.Rows; r++) for (var c = 0; c < Config.Columns; c++) if (Cells[r, c].Obstacle == null) Cells[r, c].Piece = CreatePiece(Config.Colors[InitialRandom.Next(Config.Colors.Length)], new CellPos(r, c)); if (MatchFinder.Find(this).Count == 0 && HasLegalMove()) return; } throw new InvalidOperationException("Unable to generate a stable board with a legal move."); } public bool HasLegalMove() { for (var r = 0; r < Config.Rows; r++) for (var c = 0; c < Config.Columns; c++) { var from = new CellPos(r, c); foreach (var to in Neighbors(from)) if (SwapValidator.CanSwap(this, from, to)) return true; } return false; }
        public string Snapshot()
        {
            var lines = new List<string>(); for (var r = 0; r < Config.Rows; r++) { var chars = new char[Config.Columns]; for (var c = 0; c < Config.Columns; c++) chars[c] = Cells[r, c].Obstacle != null ? 'X' : Cells[r, c].Piece == null ? '.' : ToChar(Cells[r, c].Piece.Color); lines.Add(new string(chars)); } return string.Join("\n", lines);
        }
        private static char ToChar(PieceColor color) { return color == PieceColor.Red ? 'R' : color == PieceColor.Blue ? 'B' : color == PieceColor.Green ? 'G' : color == PieceColor.Yellow ? 'Y' : 'P'; }
    }

    public class ResolveEvent
    {
        public ResolveEventType EventType; public int TurnId; public int ChainDepth; public int SourcePieceId; public CellPos? TargetCell; public PieceColor? PieceColor; public int RemainingObstacleDurability = -1; public List<CellPos> AffectedCells = new List<CellPos>();
        public ResolveEvent(ResolveEventType type, int turnId, int chainDepth = 0) { EventType = type; TurnId = turnId; ChainDepth = chainDepth; }
    }

    public class ResolveSummary
    {
        public int TurnId, ScoreDelta, ChainCount, RemainingMoves, RemainingAreaTools, DestroyedObstacles; public Dictionary<PieceColor, int> ClearedByColor = new Dictionary<PieceColor, int>(); public string FinalSnapshot;
    }

    public static class MatchFinder
    {
        public static List<CellPos> Find(BoardModel board)
        {
            var result = new HashSet<CellPos>();
            for (var r = 0; r < board.Config.Rows; r++) for (var c = 0; c < board.Config.Columns; c++) { var color = board.Cells[r, c].Piece == null ? (PieceColor?)null : board.Cells[r, c].Piece.Color; if (!color.HasValue) continue;
                if (c == 0 || board.Cells[r, c - 1].Piece == null || board.Cells[r, c - 1].Piece.Color != color.Value) { var end = c; while (end < board.Config.Columns && board.Cells[r, end].Piece != null && board.Cells[r, end].Piece.Color == color.Value) end++; if (end - c >= 3) for (var x = c; x < end; x++) result.Add(new CellPos(r, x)); }
                if (r == 0 || board.Cells[r - 1, c].Piece == null || board.Cells[r - 1, c].Piece.Color != color.Value) { var end = r; while (end < board.Config.Rows && board.Cells[end, c].Piece != null && board.Cells[end, c].Piece.Color == color.Value) end++; if (end - r >= 3) for (var x = r; x < end; x++) result.Add(new CellPos(x, c)); }
            }
            return result.ToList();
        }
    }

    public static class SwapValidator
    {
        public static bool IsAdjacent(CellPos a, CellPos b) { return Math.Abs(a.Row - b.Row) + Math.Abs(a.Column - b.Column) == 1; }
        public static bool CanSwap(BoardModel board, CellPos from, CellPos to)
        {
            if (!board.Config.IsInBounds(from) || !board.Config.IsInBounds(to) || !IsAdjacent(from, to)) return false; var a = board.Cell(from); var b = board.Cell(to); if (a.Piece == null || b.Piece == null || a.Obstacle != null || b.Obstacle != null) return false;
            var temp = a.Piece; a.Piece = b.Piece; b.Piece = temp; var valid = MatchFinder.Find(board).Count > 0; temp = a.Piece; a.Piece = b.Piece; b.Piece = temp; return valid;
        }
    }

    public class BoardState
    {
        public Piece[,] Pieces; public Obstacle[,] Obstacles; public int Moves, AreaTools, Score, NextPieceId; public uint RandomState, ShuffleState; public int RandomIndex, ShuffleIndex; public Dictionary<PieceColor, int> Cleared;
        public static BoardState Capture(BoardModel board)
        {
            var state = new BoardState { Pieces = new Piece[board.Config.Rows, board.Config.Columns], Obstacles = new Obstacle[board.Config.Rows, board.Config.Columns], Moves = board.MovesRemaining, AreaTools = board.AreaToolsRemaining, Score = board.Score, NextPieceId = board.NextPieceId, RandomState = board.RefillRandom.State, RandomIndex = board.RefillRandom.Index, ShuffleState = board.ShuffleRandom.State, ShuffleIndex = board.ShuffleRandom.Index, Cleared = new Dictionary<PieceColor, int>(board.ClearedByColor) };
            for (var r = 0; r < board.Config.Rows; r++) for (var c = 0; c < board.Config.Columns; c++) { state.Pieces[r, c] = board.Cells[r, c].Piece == null ? null : board.Cells[r, c].Piece.Clone(); state.Obstacles[r, c] = board.Cells[r, c].Obstacle == null ? null : board.Cells[r, c].Obstacle.Clone(); } return state;
        }
        public void Restore(BoardModel board)
        {
            for (var r = 0; r < board.Config.Rows; r++) for (var c = 0; c < board.Config.Columns; c++) { board.Cells[r, c].Piece = Pieces[r, c] == null ? null : Pieces[r, c].Clone(); board.Cells[r, c].Obstacle = Obstacles[r, c] == null ? null : Obstacles[r, c].Clone(); }
            board.MovesRemaining = Moves; board.AreaToolsRemaining = AreaTools; board.Score = Score; board.NextPieceId = NextPieceId; board.RefillRandom.Restore(RandomState, RandomIndex); board.ShuffleRandom.Restore(ShuffleState, ShuffleIndex); board.ClearedByColor.Clear(); foreach (var pair in Cleared) board.ClearedByColor[pair.Key] = pair.Value;
        }
    }

    public class ResolveResult { public bool IsValid; public List<ResolveEvent> Events = new List<ResolveEvent>(); public ResolveSummary Summary; }

    [Obsolete("Use MvpRulePipeline through TurnResolver.")]
    public static class ResolveSystem
    {
        public static ResolveResult Swap(BoardModel board, CellPos from, CellPos to, int turnId)
        {
            var before = BoardState.Capture(board); if (!SwapValidator.CanSwap(board, from, to)) return Invalid(turnId, board); SwapPieces(board, from, to); board.MovesRemaining--; var result = new ResolveResult { IsValid = true }; result.Events.Add(new ResolveEvent(ResolveEventType.Swap, turnId)); var depth = 1; var cleared = 0; var destroyed = 0;
            while (depth <= board.Config.MaxChainDepth) { var matches = MatchFinder.Find(board); if (matches.Count == 0) break; cleared += Clear(board, matches, result.Events, turnId, depth, ref destroyed); Gravity(board, result.Events, turnId); depth++; }
            if (result.Events.Count > board.Config.MaxEvents) { before.Restore(board); throw new InvalidOperationException("Rule error: event queue limit exceeded."); } result.Summary = Summary(board, turnId, cleared, Math.Max(0, depth - 1), destroyed); return result;
        }
        public static ResolveResult AreaTool(BoardModel board, CellPos center, int turnId)
        {
            var cells = new List<CellPos>(); for (var r = center.Row - 2; r <= center.Row + 2; r++) for (var c = center.Column - 2; c <= center.Column + 2; c++) { var pos = new CellPos(r, c); if (board.Config.IsInBounds(pos)) cells.Add(pos); } var matches = cells.Where(p => board.Cell(p).Piece != null).ToList(); if (matches.Count == 0) return Invalid(turnId, board); var events = new List<ResolveEvent>(); var destroyed = 0; var cleared = Clear(board, matches, events, turnId, 1, ref destroyed); Gravity(board, events, turnId); return new ResolveResult { IsValid = true, Events = events, Summary = Summary(board, turnId, cleared, 1, destroyed) };
        }
        private static ResolveResult Invalid(int turnId, BoardModel board) { return new ResolveResult { IsValid = false, Summary = Summary(board, turnId, 0, 0, 0) }; }
        private static ResolveSummary Summary(BoardModel board, int turnId, int cleared, int chains, int destroyed) { return new ResolveSummary { TurnId = turnId, ScoreDelta = cleared * 10, ChainCount = chains, RemainingMoves = board.MovesRemaining, DestroyedObstacles = destroyed, ClearedByColor = new Dictionary<PieceColor, int>(board.ClearedByColor), FinalSnapshot = board.Snapshot() }; }
        private static void SwapPieces(BoardModel board, CellPos from, CellPos to) { var a = board.Cell(from); var b = board.Cell(to); var piece = a.Piece; a.Piece = b.Piece; b.Piece = piece; a.Piece.LogicalPos = from; b.Piece.LogicalPos = to; }
        private static int Clear(BoardModel board, List<CellPos> matches, List<ResolveEvent> events, int turnId, int depth, ref int destroyed)
        {
            var cleared = 0; var unique = matches.Distinct().ToList(); foreach (var pos in unique) { var cell = board.Cell(pos); if (cell.Piece == null) continue; board.ClearedByColor[cell.Piece.Color]++; cell.Piece = null; cleared++; foreach (var neighbor in board.Neighbors(pos)) { var target = board.Cell(neighbor); if (target.Obstacle != null && target.Obstacle.Damage(1)) { target.Obstacle = null; destroyed++; var damage = new ResolveEvent(ResolveEventType.Damage, turnId, depth) { TargetCell = neighbor }; events.Add(damage); } } } board.Score += cleared * 10 * depth; var clear = new ResolveEvent(ResolveEventType.Clear, turnId, depth); clear.AffectedCells.AddRange(unique); events.Add(clear); return cleared;
        }
        private static void Gravity(BoardModel board, List<ResolveEvent> events, int turnId)
        {
            for (var c = 0; c < board.Config.Columns; c++) { for (var r = board.Config.Rows - 1; r >= 0; r--) { if (board.Cells[r, c].Obstacle != null || board.Cells[r, c].Piece != null) continue; var source = r - 1; while (source >= 0 && (board.Cells[source, c].Obstacle != null || board.Cells[source, c].Piece == null)) source--; if (source >= 0) { var piece = board.Cells[source, c].Piece; board.Cells[source, c].Piece = null; board.Cells[r, c].Piece = piece; piece.LogicalPos = board.Cells[r, c].Pos; var fall = new ResolveEvent(ResolveEventType.Fall, turnId) { SourcePieceId = piece.PieceId, TargetCell = board.Cells[r, c].Pos }; events.Add(fall); } } for (var r = 0; r < board.Config.Rows; r++) if (board.Cells[r, c].Obstacle == null && board.Cells[r, c].Piece == null) { var piece = board.CreatePiece(board.Config.Colors[board.RefillRandom.Next(board.Config.Colors.Length)], board.Cells[r, c].Pos); board.Cells[r, c].Piece = piece; var refill = new ResolveEvent(ResolveEventType.Refill, turnId) { SourcePieceId = piece.PieceId, TargetCell = piece.LogicalPos }; events.Add(refill); } }
        }
    }
}
