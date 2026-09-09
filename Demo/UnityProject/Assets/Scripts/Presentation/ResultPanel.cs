using UnityEngine;

namespace RedSea.Match3.Presentation
{
    public sealed class ResultPanel : MonoBehaviour
    {
        public ResultPanelModel Model { get; private set; } = new ResultPanelModel();
        public void Bind(ResultPanelModel model) { Model = model ?? new ResultPanelModel(); }

        public void Draw()
        {
            if (Model == null || !Model.Visible) return;
            var title = Model.State == RedSea.Match3.Core.GameState.Win ? "VICTORY" : "DEFEAT";
            GUI.Box(new Rect(675, 300, 520, 220), GUIContent.none);
            GUI.Label(new Rect(710, 325, 450, 40), title, new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(710, 380, 450, 28), "SCORE " + Model.Score + "   GOAL " + Model.GoalProgress + "/" + Model.GoalTarget);
            GUI.Label(new Rect(710, 415, 450, 28), "MOVES LEFT " + Model.RemainingMoves);
            GUI.Label(new Rect(710, 460, 450, 28), "点击重开按钮开始新的回合");
        }
    }
}
