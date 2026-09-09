using UnityEngine;

namespace RedSea.Match3.Testing
{
    public sealed class FixedTestRunner : MonoBehaviour
    {
        public bool runOnStart = true;
        public bool LastPassed { get; private set; }

        private void Start()
        {
            if (runOnStart) Run();
        }

        public void Run()
        {
            var report = FixedTestScenario.Run();
            LastPassed = report.Passed;
            var prefix = report.Passed ? "[FixedTest] PASS " : "[FixedTest] FAIL ";
            Debug.Log(prefix + report.Message + " events=" + report.EventCount + " snapshot=" + report.FinalSnapshot);
            if (!report.Passed) Debug.LogError("[FixedTest] " + report.Message);
        }
    }
}
