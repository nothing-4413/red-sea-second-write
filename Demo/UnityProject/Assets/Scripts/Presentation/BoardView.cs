using System.Collections.Generic;
using UnityEngine;
using RedSea.Match3.Core;

namespace RedSea.Match3.Presentation
{
    public sealed class BoardView : MonoBehaviour
    {
        public float cellSize = 64f;
        private BoardModel board;
        private readonly Dictionary<PieceColor, Texture2D> candyTextures = new Dictionary<PieceColor, Texture2D>();
        private Texture2D crateTexture, tileTexture;
        private ResolveEvent lastEvent;
        private int consumedEventCount;
        public int ConsumedEventCount { get { return consumedEventCount; } }
        public void Bind(BoardModel model)
        {
            board = model;
            candyTextures[PieceColor.Red] = Resources.Load<Texture2D>("Art/candy-red");
            candyTextures[PieceColor.Blue] = Resources.Load<Texture2D>("Art/candy-blue");
            candyTextures[PieceColor.Green] = Resources.Load<Texture2D>("Art/candy-green");
            candyTextures[PieceColor.Yellow] = Resources.Load<Texture2D>("Art/candy-yellow");
            candyTextures[PieceColor.Purple] = Resources.Load<Texture2D>("Art/candy-purple");
            crateTexture = Resources.Load<Texture2D>("Art/obstacle-crate"); tileTexture = Resources.Load<Texture2D>("Art/tile");
        }
        public CellPos? HitTest(Vector2 guiPosition)
        {
            var origin = new Vector2(30f, 90f); var x = Mathf.FloorToInt((guiPosition.x - origin.x) / cellSize); var y = Mathf.FloorToInt((guiPosition.y - origin.y) / cellSize); var row = y;
            var pos = new CellPos(row, x); return board.Config.IsInBounds(pos) ? pos : (CellPos?)null;
        }
        public void Draw(CellPos? selected)
        {
            if (board == null) return; var origin = new Vector2(30f, 90f);
            for (var row = 0; row < board.Config.Rows; row++) for (var column = 0; column < board.Config.Columns; column++) { var pos = new CellPos(row, column); var rect = new Rect(origin.x + column * cellSize, origin.y + row * cellSize, cellSize - 3f, cellSize - 3f); GUI.DrawTexture(rect, tileTexture, ScaleMode.StretchToFill); var cell = board.Cell(pos); if (cell.Obstacle != null) GUI.DrawTexture(rect, crateTexture, ScaleMode.ScaleToFit); else if (cell.Piece != null) { GUI.DrawTexture(rect, candyTextures[cell.Piece.Color], ScaleMode.ScaleToFit); GUI.Label(rect, ColorLetter(cell.Piece.Color), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, normal = { textColor = Color.white } }); } if (selected.HasValue && selected.Value == pos) GUI.Box(rect, GUIContent.none); }
            if (lastEvent != null) foreach (var pos in lastEvent.AffectedCells) GUI.Box(new Rect(origin.x + pos.Column * cellSize, origin.y + pos.Row * cellSize, cellSize - 3f, cellSize - 3f), GUIContent.none);
        }
        public void ApplyEvent(ResolveEvent item) { lastEvent = item; consumedEventCount++; }
        public void SyncFromModel() { if (board != null) lastEvent = null; }
        private static string ColorLetter(PieceColor color) { return color == PieceColor.Red ? "R" : color == PieceColor.Blue ? "B" : color == PieceColor.Green ? "G" : color == PieceColor.Yellow ? "Y" : "P"; }
    }
}
