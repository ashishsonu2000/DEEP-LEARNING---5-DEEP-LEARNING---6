using System;
using System.Collections.Concurrent;
using System.Linq;

namespace EazyEdu.Web.Services
{
    /// <summary>
    /// Sliding-window limiter keyed by session (or IP for anonymous callers).
    /// Keeps a runaway script from hammering the chat endpoint.
    /// </summary>
    public class RateLimiter
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _hits =
            new ConcurrentDictionary<string, ConcurrentQueue<DateTime>>(StringComparer.Ordinal);

        private readonly int _limit;
        private readonly TimeSpan _window;
        private DateTime _lastSweepUtc = DateTime.UtcNow;

        public RateLimiter(int limit, TimeSpan window)
        {
            _limit = limit;
            _window = window;
        }

        public bool TryAcquire(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "anonymous";

            DateTime now = DateTime.UtcNow;
            ConcurrentQueue<DateTime> queue = _hits.GetOrAdd(key, _ => new ConcurrentQueue<DateTime>());

            DateTime stale;
            while (queue.TryPeek(out stale) && now - stale > _window)
                queue.TryDequeue(out stale);

            if (queue.Count >= _limit) return false;

            queue.Enqueue(now);
            Sweep(now);
            return true;
        }

        /// <summary>Drops idle buckets every few minutes so the dictionary cannot grow without bound.</summary>
        private void Sweep(DateTime now)
        {
            if (now - _lastSweepUtc < TimeSpan.FromMinutes(5)) return;
            _lastSweepUtc = now;

            foreach (string key in _hits.Keys.ToList())
            {
                ConcurrentQueue<DateTime> queue;
                if (!_hits.TryGetValue(key, out queue)) continue;

                DateTime newest;
                if (queue.IsEmpty || (queue.TryPeek(out newest) && now - newest > _window))
                {
                    ConcurrentQueue<DateTime> removed;
                    _hits.TryRemove(key, out removed);
                }
            }
        }
    }
}
