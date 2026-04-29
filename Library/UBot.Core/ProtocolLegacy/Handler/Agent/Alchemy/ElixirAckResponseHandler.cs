using UBot.Core.Network;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Alchemy;

internal class ElixirAckResponseHandler
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


