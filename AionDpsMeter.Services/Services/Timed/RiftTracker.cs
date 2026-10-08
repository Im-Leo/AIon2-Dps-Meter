using AionDpsMeter.Core.GameData.Repositories;
using AionDpsMeter.Core.GameData.Services;

namespace AionDpsMeter.Services.Services.Timed
{
    /// <summary>
    /// Spacetime Rift state: waiting for the next spawn, open to enter, or the user inside. Going through a portal moves the
    /// client to the other faction's server, and being ported back moves it again; the game sends neither duration, so both
    /// run from their start (the spawn, or the transfer into the rift) using spacetime_rift.json.
    /// </summary>
    public sealed class RiftTracker
    {
        public enum RiftPhase { Waiting, Open, Inside }

        public readonly record struct RiftState(RiftPhase Phase, DateTime? UntilUtc);

        private readonly Lock gate = new();
        private DateTime? portalSpawnUtc;
        private DateTime? enteredUtc;

        // A rift can be entered once per opening: after leaving it, that opening no longer counts as open.
        private DateTime? enteredOpeningUtc;

        /// <summary>The client was moved to another server: into the rift through an open portal, or back out of it.</summary>
        public void OnServerTransfer(DateTime utc)
        {
            lock (gate)
            {
                var schedule = GameDataProvider.Instance.SpacetimeRift;
                if (enteredUtc is { } entered && utc < entered + schedule.StayDuration)
                    enteredUtc = null;
                else if (GetOpening(utc, schedule) is { } opening)
                {
                    enteredUtc = utc;
                    enteredOpeningUtc = opening;
                }
            }
        }

        /// <summary>A map object list contained a rift portal spawned at <paramref name="spawnUtc"/>.</summary>
        public void OnPortalSeen(DateTime spawnUtc)
        {
            lock (gate) portalSpawnUtc = spawnUtc;
        }

        public RiftState GetState(DateTime nowUtc)
        {
            var schedule = GameDataProvider.Instance.SpacetimeRift;

            lock (gate)
            {
                if (enteredUtc is { } entered && nowUtc < entered + schedule.StayDuration)
                    return new RiftState(RiftPhase.Inside, entered + schedule.StayDuration);

                enteredUtc = null;
                return GetOpening(nowUtc, schedule) is { } spawn
                    ? new RiftState(RiftPhase.Open, spawn + schedule.EntryWindow)
                    : new RiftState(RiftPhase.Waiting, schedule.NextOccurrenceUtc(nowUtc));
            }
        }

        // The spawn of the opening in progress and not yet entered, if any: the portals' own spawn time when seen for it,
        // else the schedule's.
        private DateTime? GetOpening(DateTime nowUtc, EventScheduleRepository schedule)
        {
            var spawn = portalSpawnUtc is { } seen && nowUtc - seen < schedule.EntryWindow
                ? seen
                : schedule.NextOccurrenceUtc(nowUtc - schedule.EntryWindow);
            return spawn is { } s && s <= nowUtc && s != enteredOpeningUtc ? s : null;
        }
    }
}
