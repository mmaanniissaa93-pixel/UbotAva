using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Exchange;

internal class ExchangeCanceledResponse 
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





