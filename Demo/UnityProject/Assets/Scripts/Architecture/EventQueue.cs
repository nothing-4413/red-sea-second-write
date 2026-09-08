using System;
using System.Collections.Generic;
using RedSea.Match3.Core;

namespace RedSea.Match3.Architecture
{
    public sealed class EventQueue
    {
        private readonly Queue<ResolveEvent> queue = new Queue<ResolveEvent>();
        private readonly int maxEvents;

        public EventQueue(int maxEvents) { this.maxEvents = maxEvents; }
        public int Count { get { return queue.Count; } }
        public void EnqueueRange(IEnumerable<ResolveEvent> events)
        {
            foreach (var item in events)
            {
                if (queue.Count >= maxEvents) throw new InvalidOperationException("Event queue limit exceeded.");
                queue.Enqueue(item);
            }
        }
        public bool TryDequeue(out ResolveEvent item)
        {
            if (queue.Count == 0) { item = null; return false; }
            item = queue.Dequeue(); return true;
        }
        public void Clear() { queue.Clear(); }
    }
}
