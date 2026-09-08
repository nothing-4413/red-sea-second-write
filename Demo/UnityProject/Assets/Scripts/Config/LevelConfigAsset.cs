using System.Collections.Generic;
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
        public PieceColor[] colors = { PieceColor.Red, PieceColor.Blue, PieceColor.Green, PieceColor.Yellow, PieceColor.Purple };
        [Min(1)] public int maxChainDepth = 20, maxEvents = 500, maxShuffleAttempts = 8;
        public bool enableShuffle = true;
        public bool enableRocket, enableBomb, enableFlyingBomb, enableColorBomb, enableSpecialCombo;
        public PieceColor goalColor = PieceColor.Red;
        [Min(1)] public int goalCount = 18;
        public List<ObstacleDefinition> obstacles = new List<ObstacleDefinition> { new ObstacleDefinition(3, 4, 2), new ObstacleDefinition(5, 4, 1) };
        public LevelConfig ToCore()
        {
            return new LevelConfig { Rows = rows, Columns = columns, Moves = moves, AreaToolCount = areaToolCount, Seed = seed, Colors = colors == null ? null : (PieceColor[])colors.Clone(), MaxChainDepth = maxChainDepth, MaxEvents = maxEvents, MaxShuffleAttempts = maxShuffleAttempts, EnableShuffle = enableShuffle, EnableRocket = enableRocket, EnableBomb = enableBomb, EnableFlyingBomb = enableFlyingBomb, EnableColorBomb = enableColorBomb, EnableSpecialCombo = enableSpecialCombo, GoalColor = goalColor, GoalCount = goalCount, Obstacles = new List<ObstacleDefinition>(obstacles) };
        }
    }
}
