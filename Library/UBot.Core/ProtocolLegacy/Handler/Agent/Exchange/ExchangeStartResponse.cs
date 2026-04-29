using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects.Exchange;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Exchange;

internal class ExchangeStartResponse 
{
    /// <inheritdoc />
    public ushort Opcode => 0xB081;

    /// <inheritdoc />
    public PacketDestination Destination => PacketDestination.Client;

    /// <inheritdoc />
    public void Invoke(Packet packet)
    {
        if (packet.ReadByte() != 1)
            return;

        var playerUniqueId = packet.ReadUInt();
        CoreGame.Player.Exchange = new ExchangeInstance(playerUniqueId);

        Log.Notify($"Started exchanging with the player {CoreGame.Player.Exchange.ExchangePlayer.Name}");

        EventManager.FireEvent("OnStartExchange");
    }
}





