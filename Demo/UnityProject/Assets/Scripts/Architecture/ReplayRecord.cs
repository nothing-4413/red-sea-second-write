using System.Collections.Generic;
using RedSea.Match3.Core;

namespace RedSea.Match3.Architecture
{
    public sealed class ReplayRecord
    {
        public const string RuleVersion = "1.7";
        public int Seed;
        public string InitialSnapshot;
        public List<ReplayInput> Inputs = new List<ReplayInput>();
        public List<ResolveSummary> Summaries = new List<ResolveSummary>();
    }

    public struct ReplayInput
    {
        public CellPos From; public CellPos To;
        public ReplayInput(CellPos from, CellPos to) { From = from; To = to; }
    }
}
