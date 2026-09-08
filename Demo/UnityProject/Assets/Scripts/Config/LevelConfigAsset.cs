using UnityEngine;
using RedSea.Match3.Core;

namespace RedSea.Match3.Config
{
    [CreateAssetMenu(menuName = "Red Sea/Level Config", fileName = "LevelConfig_001")]
    public sealed class LevelConfigAsset : ScriptableObject
    {
        [Min(1)] public int rows = 9, columns = 9;
        [Min(0)] public int moves = 24, areaToolCount = 2;
        public int seed = 202603;
        public bool enableShuffle = true;
        public PieceColor goalColor = PieceColor.Red;
        [Min(1)] public int goalCount = 18;
        public LevelConfig ToCore()
        {
            return new LevelConfig { Rows = rows, Columns = columns, Moves = moves, AreaToolCount = areaToolCount, Seed = seed, EnableShuffle = enableShuffle, GoalColor = goalColor, GoalCount = goalCount };
        }
    }
}
