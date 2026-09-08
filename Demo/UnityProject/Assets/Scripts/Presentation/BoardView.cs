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
        public CellPos? HitTest(Vector2 screenPosition)
        {
            var origin = new Vector2(30f, Screen.height - 90f - board.Config.Rows * cellSize); var x = Mathf.FloorToInt((screenPosition.x - origin.x) / cellSize); var y = Mathf.FloorToInt((screenPosition.y - origin.y) / cellSize); var row = board.Config.Rows - 1 - y;
            var pos = new CellPos(row, x); return board.Config.IsInBounds(pos) ? pos : (CellPos?)null;
        }
        public void Draw(CellPos? selected)
        {
            if (board == null) return; var origin = new Vector2(30f, Screen.height - 90f - board.Config.Rows * cellSize);
            for (var row = 0; row < board.Config.Rows; row++) for (var column = 0; column < board.Config.Columns; column++) { var pos = new CellPos(row, column); var rect = new Rect(origin.x + column * cellSize, origin.y + (board.Config.Rows - 1 - row) * cellSize, cellSize - 3f, cellSize - 3f); GUI.DrawTexture(rect, tileTexture, ScaleMode.StretchToFill); var cell = board.Cell(pos); if (cell.Obstacle != null) GUI.DrawTexture(rect, crateTexture, ScaleMode.ScaleToFit); else if (cell.Piece != null) { GUI.DrawTexture(rect, candyTextures[cell.Piece.Color], ScaleMode.ScaleToFit); GUI.Label(rect, ColorLetter(cell.Piece.Color), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, normal = { textColor = Color.white } }); } if (selected.HasValue && selected.Value == pos) GUI.Box(rect, GUIContent.none); }
        }
        private static string ColorLetter(PieceColor color) { return color == PieceColor.Red ? "R" : color == PieceColor.Blue ? "B" : color == PieceColor.Green ? "G" : color == PieceColor.Yellow ? "Y" : "P"; }
    }
}
