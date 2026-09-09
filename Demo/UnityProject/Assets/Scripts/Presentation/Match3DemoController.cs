using System.Collections;
using UnityEngine;
using RedSea.Match3.Core;
using RedSea.Match3.Flow;
using RedSea.Match3.Config;
using RedSea.Match3.Architecture;

namespace RedSea.Match3.Presentation
{
    public sealed class Match3DemoController : MonoBehaviour
    {
        public LevelConfigAsset levelConfig; public BoardView boardView; public EffectPlayer effectPlayer;
        private BoardModel board; private TurnResolver resolver; private readonly TurnStateMachine stateMachine = new TurnStateMachine(); private InputController inputController; private string toast = "点击两个相邻糖果开始交换"; private ResolveSummary lastSummary; private TurnPerformanceTrace currentPerformance; private Font displayFont;
        private void Awake() { if (boardView == null) boardView = FindObjectOfType<BoardView>(); if (effectPlayer == null) effectPlayer = FindObjectOfType<EffectPlayer>(); if (effectPlayer == null) effectPlayer = new GameObject("EffectPlayer").AddComponent<EffectPlayer>(); displayFont = Resources.Load<Font>("Fonts/display"); stateMachine.StateChanged += state => Debug.Log("[Turn] " + state); Initialize(); }
        private void Initialize() { stateMachine.Reset(); var config = levelConfig == null ? new LevelConfig() : levelConfig.ToCore(); board = new BoardModel(config); resolver = new TurnResolver(board); inputController = new InputController(board, resolver, stateMachine); boardView.Bind(board); }
        private void OnGUI()
        {
            GUI.Label(new Rect(30, 18, 620, 32), "红海二笔 · MATCH-3 ARCHITECTURE DEMO", new GUIStyle(GUI.skin.label) { font = displayFont, fontSize = 22, fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(700, 22, 500, 28), "STATE: " + stateMachine.State + "   TURN: " + inputController?.TurnId + "   SEED: " + board?.RefillRandom.State);
            GUI.Label(new Rect(700, 58, 500, 28), "MOVES " + board?.MovesRemaining + "   SCORE " + board?.Score + "   RED GOAL " + GoalProgress());
            GUI.Label(new Rect(700, 94, 540, 44), toast);
            if (GUI.Button(new Rect(700, 150, 180, 48), "5×5 道具 (" + board.AreaToolsRemaining + ")")) UseAreaTool();
            if (GUI.Button(new Rect(900, 150, 130, 48), "重开")) Restart();
            if (GUI.Button(new Rect(1050, 150, 130, 48), "回放固定操作")) StartCoroutine(Replay());
            boardView.Draw(inputController?.Selected); HandleInput();
            if (lastSummary != null) GUI.Label(new Rect(700, 230, 540, 130), "RESOLVE SUMMARY\nchain=" + lastSummary.ChainCount + "  cleared=" + lastSummary.ClearedByColor[PieceColor.Red] + "  scoreDelta=" + lastSummary.ScoreDelta + "\n" + lastSummary.FinalSnapshot);
        }
        private void HandleInput()
        {
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
            var hit = boardView.HitTest(Event.current.mousePosition);
            if (hit.HasValue) HandleBoardInput(inputController.Click(hit.Value));
        }
        private void HandleBoardInput(BoardInputResult result) { switch (result.Type) { case BoardInputResultType.Selected: toast = "已选择 " + result.Selection + "，请选择相邻糖果"; break; case BoardInputResultType.Reselected: toast = "已改选 " + result.Selection + "（必须点击上下左右相邻格）"; break; case BoardInputResultType.Cancelled: toast = "已取消选择"; break; case BoardInputResultType.ObstacleBlocked: toast = "障碍物不可交换"; break; case BoardInputResultType.InvalidSwap: toast = "无效交换：已完整回滚，步数与随机流不变"; break; case BoardInputResultType.ToolUnavailable: toast = "道具不可用"; break; case BoardInputResultType.InvalidToolTarget: toast = "道具目标无效"; break; case BoardInputResultType.SwapCommitted: case BoardInputResultType.ToolCommitted: BeginPresentation(result.Resolution); break; } }
        private void BeginPresentation(ResolveResult result) { lastSummary = result.Summary; currentPerformance = result.Performance; ConsumeEvents(); }
        private void ConsumeEvents() { effectPlayer.Play(resolver, boardView, currentPerformance, OnEventsCompleted, OnEventsTimedOut); }
        private void OnEventsCompleted() { stateMachine.BeginRefill(); stateMachine.CheckChain(); var endState = resolver.EvaluateEndState(); if (endState == GameState.Win || endState == GameState.Lose) stateMachine.Finish(endState); else stateMachine.ReturnToIdle(); LogPerformance(); toast = "棋盘稳定，输入已解锁"; }
        private void OnEventsTimedOut(ErrorSnapshot error) { var endState = resolver.EvaluateEndState(); if (endState == GameState.Win || endState == GameState.Lose) stateMachine.Finish(endState); else stateMachine.ReturnToIdle(); LogPerformance(); if (ErrorSnapshotExporter.TryExport(error, Application.persistentDataPath, out var path, out var exportError)) { Debug.LogError("[Presentation] " + error.Message + " turn=" + error.TurnId + " snapshot=" + path); toast = "表现播放超时，快照已导出：" + path; } else { Debug.LogError("[Presentation] Snapshot export failed: " + exportError + " turn=" + error.TurnId + " board=" + error.BoardSnapshot); toast = "表现播放超时，快照导出失败；已同步逻辑棋盘"; } }
        private void UseAreaTool() { var center = inputController.Selected ?? new CellPos(board.Config.Rows / 2, board.Config.Columns / 2); HandleBoardInput(inputController.UseAreaTool(center)); }
        private int GoalProgress() { return board == null ? 0 : board.ClearedByColor[board.Config.GoalColor]; }
        private void Restart() { StopAllCoroutines(); if (effectPlayer != null) effectPlayer.StopPlayback(); lastSummary = null; currentPerformance = null; Initialize(); toast = "已重开，固定 seed=" + board.Config.Seed; }
        private void LogPerformance() { if (currentPerformance == null) return; Debug.Log("[Performance] turn=" + inputController.TurnId + " logicMs=" + currentPerformance.LogicMilliseconds.ToString("F3") + " matchMs=" + currentPerformance.MatchScanMilliseconds.ToString("F3") + " specialMs=" + currentPerformance.SpecialMilliseconds.ToString("F3") + " resolveMs=" + currentPerformance.ResolveMilliseconds.ToString("F3") + " gravityRefillMs=" + currentPerformance.GravityRefillMilliseconds.ToString("F3") + " shuffleMs=" + currentPerformance.ShuffleMilliseconds.ToString("F3") + " playbackMs=" + currentPerformance.EventPlaybackMilliseconds.ToString("F3")); }
        private IEnumerator Replay() { Restart(); yield return new WaitForSeconds(0.2f); if (resolver.TryFindLegalMove(out var from, out var to)) HandleBoardInput(inputController.SubmitSwap(from, to)); else toast = "回放失败：规则层未找到合法交换"; }
    }
}
