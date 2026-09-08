using RedSea.Match3.Core;

namespace RedSea.Match3.Core.Rules
{
    public interface IRulePipeline
    {
        ResolveResult ResolveSwap(BoardModel board, CellPos from, CellPos to, int turnId);
        ResolveResult ResolveAreaTool(BoardModel board, CellPos center, int turnId);
    }

    public sealed class MvpRulePipelineAdapter : IRulePipeline
    {
        public ResolveResult ResolveSwap(BoardModel board, CellPos from, CellPos to, int turnId)
        {
            return MvpRulePipeline.ResolveSwap(board, from, to, turnId);
        }

        public ResolveResult ResolveAreaTool(BoardModel board, CellPos center, int turnId)
        {
            return MvpRulePipeline.ResolveAreaTool(board, center, turnId);
        }
    }
}
