using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeCanceledResponse : IPacketHandler 
{
    /// <inheritdoc />
    public ushort Opcode => 0x3088;

    /// <inheritdoc />
    public PacketDestination Destination => PacketDestination.Client;

    /// <inheritdoc />
    public void Invoke(Packet packet)
    {
        CoreGame.Player.Exchange = null;

        Log.Notify("Exchange has been canceled.");

        EventManager.FireEvent("OnCancelExchange");
    }
}





