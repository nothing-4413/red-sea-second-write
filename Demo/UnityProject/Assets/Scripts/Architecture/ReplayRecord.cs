using System.Collections.Generic;
using RedSea.Match3.Core;

namespace RedSea.Match3.Architecture
{
    public sealed class ReplayRecord
    {
        public const string RuleVersion = "1.7";
        public string RecordedRuleVersion = RuleVersion;
        public int Seed;
        public string InitialSnapshot;
        public int InitialRandomIndex;
        public int InitialRefillRandomIndex;
        public int InitialShuffleRandomIndex;
        public List<ReplayInput> Inputs = new List<ReplayInput>();
        public List<ResolveSummary> Summaries = new List<ResolveSummary>();
        public bool IsCompatible(string currentRuleVersion) { return RecordedRuleVersion == currentRuleVersion; }
    }

    public enum ReplayInputType { Swap, AreaTool }

    public struct ReplayInput
    {
        public ReplayInputType Type;
        public CellPos From; public CellPos To; public CellPos Center;
        public ReplayInput(CellPos from, CellPos to) { Type = ReplayInputType.Swap; From = from; To = to; Center = new CellPos(); }
        public static ReplayInput AreaTool(CellPos center) { return new ReplayInput { Type = ReplayInputType.AreaTool, Center = center }; }
    }
}
