using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Commands.Agent.Action;

public class ActionTalkRequest : IPacketHandler
{
    #region Methods

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        var entityId = packet.ReadUInt();
        var option = (TalkOption)packet.ReadByte();

        CoreGame.Player.State.DialogState = new DialogState { RequestedNpcId = entityId, TalkOption = option };

        EventManager.FireEvent("OnTalkRequest", entityId, option);
    }

    #endregion Methods

    #region Properties

    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x7046;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Server;

    #endregion Properties
}






