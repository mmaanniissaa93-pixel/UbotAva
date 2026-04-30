using UBot.Core.Network;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Alchemy;

public class ElixirAckResponseHandler : IPacketHandler
{
    public void Invoke(Packet packet)
    {
        GenericAlchemyAckResponse.Invoke(packet, AlchemyType.Elixir);
    }

    #region Properties

    public ushort Opcode => 0xB150;

    public PacketDestination Destination => PacketDestination.Client;

    #endregion Properties
}


