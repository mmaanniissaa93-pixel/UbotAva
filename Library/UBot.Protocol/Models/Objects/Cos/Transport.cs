using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Core.Objects.Cos;

public class Transport : Cos
{
    /// <summary>
    ///     Dismounts this instance.
    /// </summary>
    public override bool Dismount()
    {
        var packet = new Packet(0x70CB);
        packet.WriteByte(0);
        packet.WriteUInt(UniqueId);

        UBot.Protocol.ProtocolRuntime.SendPacket(packet, PacketDestination.Server);

        return base.Dismount();
    }
}
