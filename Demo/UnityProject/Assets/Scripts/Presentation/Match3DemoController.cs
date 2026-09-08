using System.Collections;
using UnityEngine;
using RedSea.Match3.Core;
using RedSea.Match3.Flow;
using RedSea.Match3.Config;

namespace RedSea.Match3.Presentation
{
    public sealed class Match3DemoController : MonoBehaviour
    {
        public LevelConfigAsset levelConfig; public BoardView boardView; public float eventDelay = 0.06f;
        private BoardModel board; private readonly TurnStateMachine stateMachine = new TurnStateMachine(); private CellPos? selected; private int turnId; private int areaTools; private string toast = "点击两个相邻糖果开始交换"; private ResolveSummary lastSummary;
        private void Start() { if (boardView == null) boardView = FindObjectOfType<BoardView>(); var config = levelConfig == null ? new LevelConfig() : levelConfig.ToCore(); board = new BoardModel(config); areaTools = config.AreaToolCount; boardView.Bind(board); stateMachine.StateChanged += state => Debug.Log("[Turn] " + state); }
        private void OnGUI()
        {
            GUI.Label(new Rect(30, 18, 620, 32), "红海二笔 · MATCH-3 ARCHITECTURE DEMO", new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(700, 22, 500, 28), "STATE: " + stateMachine.State + "   TURN: " + turnId + "   SEED: " + board?.RefillRandom.State);
            GUI.Label(new Rect(700, 58, 500, 28), "MOVES " + board?.MovesRemaining + "   SCORE " + board?.Score + "   RED GOAL " + GoalProgress());
            GUI.Label(new Rect(700, 94, 540, 44), toast);
            if (GUI.Button(new Rect(700, 150, 180, 48), "5×5 道具 (" + areaTools + ")")) UseAreaTool();
            if (GUI.Button(new Rect(900, 150, 130, 48), "重开")) Restart();
            if (GUI.Button(new Rect(1050, 150, 130, 48), "回放固定操作")) StartCoroutine(Replay());
            boardView.Draw(selected); HandleInput();
            if (lastSummary != null) GUI.Label(new Rect(700, 230, 540, 130), "RESOLVE SUMMARY\nchain=" + lastSummary.ChainCount + "  cleared=" + lastSummary.ClearedByColor[PieceColor.Red] + "  scoreDelta=" + lastSummary.ScoreDelta + "\n" + lastSummary.FinalSnapshot);
        }
        private void HandleInput()
        {
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || stateMachine.InputLocked) return; var hit = boardView.HitTest(Event.current.mousePosition); if (!hit.HasValue) return; var pos = hit.Value; if (board.Cell(pos).Obstacle != null) { toast = "障碍物不可交换"; return; }
            if (!selected.HasValue) { selected = pos; stateMachine.Select(); toast = "已选择 " + pos + "，请选择相邻糖果"; return; } if (selected.Value == pos) { selected = null; stateMachine.ReturnToIdle(); toast = "已取消选择"; return; } if (!SwapValidator.IsAdjacent(selected.Value, pos)) { selected = pos; toast = "已改选 " + pos + "（必须点击上下左右相邻格）"; return; } SubmitSwap(selected.Value, pos);
        }
        private void SubmitSwap(CellPos from, CellPos to) { stateMachine.BeginResolve(); var result = ResolveSystem.Swap(board, from, to, ++turnId); selected = null; if (!result.IsValid) { stateMachine.ReturnToIdle(); toast = "无效交换：已完整回滚，步数与随机流不变"; return; } lastSummary = result.Summary; stateMachine.BeginAnimation(); StartCoroutine(ConsumeEvents(result)); }
        private IEnumerator ConsumeEvents(ResolveResult result) { foreach (var item in result.Events) { yield return new WaitForSeconds(eventDelay); Debug.Log("[Event] " + item.EventType + " turn=" + item.TurnId + " depth=" + item.ChainDepth); } stateMachine.BeginRefill(); stateMachine.CheckChain(); yield return null; if (board.MovesRemaining <= 0) stateMachine.Finish(GameState.Lose); else if (GoalProgress() >= board.Config.GoalCount) stateMachine.Finish(GameState.Win); else stateMachine.ReturnToIdle(); toast = "棋盘稳定，输入已解锁"; }
        private void UseAreaTool() { if (areaTools <= 0 || stateMachine.InputLocked) { toast = "道具不可用"; return; } var center = selected ?? new CellPos(board.Config.Rows / 2, board.Config.Columns / 2); var result = ResolveSystem.AreaTool(board, center, ++turnId); if (!result.IsValid) { toast = "道具目标无效"; return; } areaTools--; lastSummary = result.Summary; StartCoroutine(ConsumeEvents(result)); }
        private int GoalProgress() { return board == null ? 0 : board.ClearedByColor[board.Config.GoalColor]; }
        private void Restart() { StopAllCoroutines(); Start(); selected = null; lastSummary = null; toast = "已重开，固定 seed=" + board.Config.Seed; }
        private IEnumerator Replay() { Restart(); yield return new WaitForSeconds(0.2f); SubmitSwap(new CellPos(4, 3), new CellPos(4, 4)); }
    }
}
