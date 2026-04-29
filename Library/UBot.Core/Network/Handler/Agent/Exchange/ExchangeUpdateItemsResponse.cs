using UBot.Core.Event;
using UBot.Core.Objects.Exchange;

namespace UBot.Core.Network.Handler.Agent.Exchange;

internal class ExchangeUpdateItemsResponse : IPacketHandler
{
    /// <inheritdoc />
    public ushort Opcode => 0x308C;

    /// <inheritdoc />
    public PacketDestination Destination => PacketDestination.Client;

    /// <inheritdoc />
    public void Invoke(Packet packet)
    {
        Game.Player.Exchange?.UpdateItems(packet, Game.Player.UniqueId);

        EventManager.FireEvent("OnUpdateExchangeItems");
    }
}
