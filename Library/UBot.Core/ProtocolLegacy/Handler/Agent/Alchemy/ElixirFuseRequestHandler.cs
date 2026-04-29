using UBot.Core.Network;
namespace UBot.Core.ProtocolLegacy.Handler.Agent.Alchemy;

internal class ElixirFuseRequestHandler 
{
    public ushort Opcode => 0x7150;

    public PacketDestination Destination => PacketDestination.Server;

    public void Invoke(Packet packet)
    {
        GenericAlchemyRequestHandler.Invoke(packet);
    }
}

