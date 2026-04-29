using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects.Exchange;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Exchange;

internal class ExchangeStartedResponse 
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





