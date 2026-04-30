using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects.Exchange;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeStartedResponse : IPacketHandler 
{
    /// <inheritdoc />
    public ushort Opcode => 0x3085;

    /// <inheritdoc />
    public PacketDestination Destination => PacketDestination.Client;

    /// <inheritdoc />
    public void Invoke(Packet packet)
    {
        var playerUniqueId = packet.ReadUInt();
        CoreGame.Player.Exchange = new ExchangeInstance(playerUniqueId);

        Log.Notify($"Started exchanging with the player {CoreGame.Player.Exchange.ExchangePlayer.Name}");

        EventManager.FireEvent("OnStartExchange");
    }
}





