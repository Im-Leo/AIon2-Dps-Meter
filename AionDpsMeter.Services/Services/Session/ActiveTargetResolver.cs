using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class ActiveTargetResolver
    {
        private static readonly TimeSpan RecentHitWindow = TimeSpan.FromSeconds(5);
        // Matches the meter's 10 Hz read, so a skipped recompute is never visible.
        private static readonly TimeSpan RecomputeInterval = TimeSpan.FromMilliseconds(100);

        private readonly EntityTracker entityTracker;
        private DateTime? lastComputedAt;

        public int? ActiveTargetId { get; private set; }

        public ActiveTargetResolver(EntityTracker entityTracker)
        {
            this.entityTracker = entityTracker;
        }

        /// <summary>
        /// Recomputes the active target at most once per <see cref="RecomputeInterval"/> of hit time, unless
        /// <paramref name="force"/> is set or the current target is gone or finished.
        /// </summary>
        public void Update(IReadOnlyDictionary<int, TargetEntry> entries, DateTime lastHitTime, bool force = false)
        {
            bool recompute = force
                || ActiveTargetId is not { } id || !entries.TryGetValue(id, out var current) || current.CurrentSession is not { IsCompleted: false }
                || lastComputedAt is not { } at || lastHitTime < at || lastHitTime - at >= RecomputeInterval;
            if (!recompute) return;

            lastComputedAt = lastHitTime;
            ActiveTargetId = Resolve(entries.Values, lastHitTime - RecentHitWindow);
        }

        private static int? Resolve(IEnumerable<TargetEntry> entries, DateTime cutoff)
        {
            TargetEntry? userBest = null;
            DateTime userBestHit = default;
            foreach (var entry in entries)
            {
                if (entry.CurrentSession is not { IsCompleted: false }) continue;
                if (entry.GetUserLastHitTime() is { } hit && hit >= cutoff && (userBest is null || hit > userBestHit))
                {
                    userBest = entry;
                    userBestHit = hit;
                }
            }
            if (userBest is not null) return userBest.TargetId;

            TargetEntry? best = null;
            int bestCount = 0;
            foreach (var entry in entries)
            {
                if (entry.CurrentSession is not { IsCompleted: false }) continue;
                int count = entry.CountRecentHits(cutoff);
                if (count > bestCount)
                {
                    best = entry;
                    bestCount = count;
                }
            }
            return best?.TargetId;
        }

        public Mob? GetActiveTargetMob()
        {
            if (ActiveTargetId is not { } targetId) return null;
            return entityTracker.GetTargetMob(targetId);
        }

        public void Reset()
        {
            ActiveTargetId = null;
            lastComputedAt = null;
        }
    }
}
