using UBot.Core.Network;
using UBot.Protocol.Legacy;
namespace UBot.Protocol.Commands.Agent.Alchemy;

public class ElixirFuseRequestHandler : IPacketHandler 
{
    public ushort Opcode => 0x7150;

    public PacketDestination Destination => PacketDestination.Server;

    public void Invoke(Packet packet)
    {
        GenericAlchemyRequestHandler.Invoke(packet);
    }
}

