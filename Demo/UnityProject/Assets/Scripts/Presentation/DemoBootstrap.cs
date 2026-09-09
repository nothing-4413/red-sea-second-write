using UnityEngine;
using RedSea.Match3.Testing;

namespace RedSea.Match3.Presentation
{
    public static class DemoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateDemo()
        {
            if (Object.FindObjectOfType<Match3DemoController>() != null) return;
            var entryPoint = Object.FindObjectOfType<SceneEntryPoint>();
            if (entryPoint != null && entryPoint.Mode == SceneEntryMode.FixedTests)
            {
                if (Object.FindObjectOfType<FixedTestRunner>() == null) new GameObject("FixedTestRunner").AddComponent<FixedTestRunner>();
                return;
            }
            var board = new GameObject("BoardView").AddComponent<BoardView>();
            var effects = new GameObject("EffectPlayer").AddComponent<EffectPlayer>();
            var controller = new GameObject("Match3DemoController").AddComponent<Match3DemoController>();
            controller.boardView = board;
            controller.effectPlayer = effects;
        }
    }
}
