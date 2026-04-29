using UBot.Core.Network;

namespace UBot.Core.Objects.Spawn;

internal static class SpawnedBionicPacketExtensions
{
    internal static void ParseBionicDetails(this SpawnedBionic bionic, Packet packet)
    {
        bionic.UniqueId = packet.ReadUInt();

        var movement = packet.ReadMovement();
        bionic.State.Deserialize(packet);
        bionic.SetMovement(movement);
    }
}
