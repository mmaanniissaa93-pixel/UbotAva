using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeApprovedResponse : IPacketHandler 
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

        UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnApproveExchange");
    }
}





