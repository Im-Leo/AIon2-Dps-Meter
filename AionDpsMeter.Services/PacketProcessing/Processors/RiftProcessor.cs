using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Timed;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    /// <summary>
    /// Feeds the rift tracker: server transfers (entering or leaving the rift) and rift portals in the map object lists.
    /// </summary>
    [PacketOpcode(PacketOpcodes.ServerTransfer)]
    [PacketOpcode(PacketOpcodes.MapObjects)]
    internal sealed class RiftProcessor(RiftTracker riftTracker) : IOpcodeProcessor
    {
        // A map object entry: [varint id][u32 type][u32 subtype][f32 x, y, z][u8][u64 spawn time, Unix ms].
        private const int SpawnTimeOffsetFromType = 4 + 4 + 12 + 1;
        private static readonly TimeSpan MaxClockSkew = TimeSpan.FromDays(1);

        public void Process(Packet packet)
        {
            var data = packet.Data;
            var r = new PacketReader(data);
            r.ReadVarInt();
            ushort opcode = r.ReadU16();
            int bodyStart = r.Position;
            var nowUtc = DateTime.UtcNow;

            if (opcode == PacketOpcodes.ServerTransfer)
            {
                riftTracker.OnServerTransfer(nowUtc);
                return;
            }

            var portalTypes = GameDataProvider.Instance.SpacetimeRift.PortalTypes;
            for (int off = bodyStart; off + SpawnTimeOffsetFromType + 8 <= data.Length; off++)
            {
                if (!portalTypes.Contains(BitConverter.ToInt32(data, off))) continue;

                long spawnMs = BitConverter.ToInt64(data, off + SpawnTimeOffsetFromType);
                if (spawnMs <= 0 || spawnMs > DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()) continue;
                var spawnUtc = DateTimeOffset.FromUnixTimeMilliseconds(spawnMs).UtcDateTime;
                if ((spawnUtc - nowUtc).Duration() > MaxClockSkew) continue;

                riftTracker.OnPortalSeen(spawnUtc);
            }
        }
    }
}
