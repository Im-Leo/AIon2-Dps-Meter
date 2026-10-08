using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>Which rows the meter lists: only the group while grouped, otherwise everyone.</summary>
    public static class MeterRowFilter
    {
        public static List<PlayerStats> Apply(IEnumerable<PlayerStats> stats, bool isGrouped)
        {
            var all = stats.ToList();
            return isGrouped ? all.Where(s => s.IsUser || s.Group != GroupKind.None).ToList() : all;
        }
    }
}
