using RedSea.Match3.Core;

namespace RedSea.Match3.Architecture
{
    public enum GameErrorType
    {
        Configuration,
        Input,
        Rule,
        StateMachine,
        Presentation,
        Replay,
        Performance
    }

    public sealed class ErrorSnapshot
    {
        public GameErrorType ErrorType;
        public string Message;
        public int TurnId;
        public GameState State;
        public int Seed;
        public int InitialRandomIndex;
        public int RefillRandomIndex;
        public int ShuffleRandomIndex;
        public int EventQueueCount;
        public string BoardSnapshot;
    }

    public static class ErrorReporter
    {
        public static ErrorSnapshot Capture(GameErrorType type, string message, BoardModel board, GameState state, int turnId, int eventQueueCount)
        {
            return new ErrorSnapshot
            {
                ErrorType = type,
                Message = message,
                TurnId = turnId,
                State = state,
                Seed = board.Config.Seed,
                InitialRandomIndex = board.InitialRandom.Index,
                RefillRandomIndex = board.RefillRandom.Index,
                ShuffleRandomIndex = board.ShuffleRandom.Index,
                EventQueueCount = eventQueueCount,
                BoardSnapshot = board.Snapshot()
            };
        }
    }
}
