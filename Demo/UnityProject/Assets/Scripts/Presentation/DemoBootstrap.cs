using UnityEngine;

namespace RedSea.Match3.Presentation
{
    public static class DemoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateDemo()
        {
            if (Object.FindObjectOfType<Match3DemoController>() != null) return;
            var board = new GameObject("BoardView").AddComponent<BoardView>();
            var controller = new GameObject("Match3DemoController").AddComponent<Match3DemoController>();
            controller.boardView = board;
        }
    }
}
