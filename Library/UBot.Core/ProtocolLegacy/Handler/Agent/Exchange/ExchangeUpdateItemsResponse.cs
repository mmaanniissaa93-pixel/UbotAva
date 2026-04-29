using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects.Exchange;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Exchange;

internal class ExchangeUpdateItemsResponse 
{
    /// <inheritdoc />
    public ushort Opcode => 0x308C;

    /// <inheritdoc />
    public PacketDestination Destination => PacketDestination.Client;

    /// <inheritdoc />
    public void Invoke(Packet packet)
    {
        CoreGame.Player.Exchange?.UpdateItems(packet, CoreGame.Player.UniqueId);

        EventManager.FireEvent("OnUpdateExchangeItems");
    }
}





