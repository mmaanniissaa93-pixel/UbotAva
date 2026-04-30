using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Core.Objects.Spawn;

public static class SpawnedBionicPacketExtensions
{
    public static void ParseBionicDetails(this SpawnedBionic bionic, Packet packet)
    {
        bionic.UniqueId = packet.ReadUInt();

        var movement = packet.ReadMovement();
        bionic.State.Deserialize(packet);
        bionic.SetMovement(movement);
    }
}
