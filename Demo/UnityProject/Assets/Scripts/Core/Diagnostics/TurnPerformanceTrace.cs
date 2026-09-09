using System;
using System.Diagnostics;

namespace RedSea.Match3.Core
{
    public sealed class TurnPerformanceTrace
    {
        public double MatchScanMilliseconds { get; private set; }
        public double SpecialMilliseconds { get; private set; }
        public double ResolveMilliseconds { get; private set; }
        public double GravityRefillMilliseconds { get; private set; }
        public double ShuffleMilliseconds { get; private set; }
        public double LogicMilliseconds { get; private set; }
        public double EventPlaybackMilliseconds { get; private set; }
        public int MatchScanCount { get; private set; }
        public int SpecialCount { get; private set; }
        public int ResolveLayerCount { get; private set; }
        public int GravityRefillCount { get; private set; }
        public int ShuffleCount { get; private set; }
        public int EventPlaybackCount { get; private set; }

        internal void RecordMatchScan(long started) { MatchScanMilliseconds += Elapsed(started); MatchScanCount++; }
        internal void RecordResolve(long started) { ResolveMilliseconds += Elapsed(started); ResolveLayerCount++; }
        internal void RecordGravityRefill(long started) { GravityRefillMilliseconds += Elapsed(started); GravityRefillCount++; }
        internal void RecordShuffle(long started) { ShuffleMilliseconds += Elapsed(started); ShuffleCount++; }
        internal void CompleteLogic(long started) { LogicMilliseconds = Elapsed(started); }
        public void RecordEventPlayback(double milliseconds) { EventPlaybackMilliseconds += Math.Max(0, milliseconds); EventPlaybackCount++; }

        private static double Elapsed(long started)
        {
            return (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
        }
    }
}
