using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects.Job;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Job;

internal class JobUpdateTradeScaleResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x30E8;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        if (CoreGame.Player.TradeInfo == null)
            CoreGame.Player.TradeInfo = new TradeInfo();

        CoreGame.Player.TradeInfo.Scale = packet.ReadByte();

        Log.Notify($"[Job] Difficulty set to level {CoreGame.Player.TradeInfo.Scale}");

        EventManager.FireEvent("OnJobScaleUpdate");
    }
}





