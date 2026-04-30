using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects.Exchange;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeStartResponse : IPacketHandler 
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





