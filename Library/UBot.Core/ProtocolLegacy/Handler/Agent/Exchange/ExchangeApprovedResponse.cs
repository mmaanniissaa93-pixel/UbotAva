using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Exchange;

internal class ExchangeApprovedResponse 
{
    /// <inheritdoc />
    public ushort Opcode => 0x3087;

    /// <inheritdoc />
    public PacketDestination Destination => PacketDestination.Client;

    /// <inheritdoc />
    public void Invoke(Packet packet)
    {
        CoreGame.Player.Exchange.Complete();
        CoreGame.Player.Exchange = null;

        Log.Notify("Exchange completed.");

        EventManager.FireEvent("OnApproveExchange");
    }
}





