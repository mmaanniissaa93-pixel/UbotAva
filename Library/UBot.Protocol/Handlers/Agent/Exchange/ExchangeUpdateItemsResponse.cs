using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects.Exchange;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeUpdateItemsResponse : IPacketHandler 
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





