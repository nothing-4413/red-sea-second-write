using UnityEngine;

namespace RedSea.Match3.Presentation
{
    public enum SceneEntryMode
    {
        Demo,
        FixedTests
    }

    public sealed class SceneEntryPoint : MonoBehaviour
    {
        public SceneEntryMode mode = SceneEntryMode.Demo;
        public SceneEntryMode Mode { get { return mode; } }
    }
}
