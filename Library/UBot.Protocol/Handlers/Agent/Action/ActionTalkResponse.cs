using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Action;

public class ActionTalkResponse : IPacketHandler
{
    #region Methods

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        var result = packet.ReadByte();

        if (CoreGame.Player.State.DialogState == null || result != 1)
            return;

        var talkOption = (TalkOption)packet.ReadByte();

        CoreGame.Player.State.DialogState.Npc = SpawnManager.GetEntity<SpawnedNpc>(
            CoreGame.Player.State.DialogState.RequestedNpcId
        );
        CoreGame.Player.State.DialogState.TalkOption = talkOption;

        if (talkOption == TalkOption.Trade)
            CoreGame.Player.State.DialogState.IsSpecialityTime = packet.ReadBool();

        CoreGame.SelectedEntity = CoreGame.Player.State.DialogState.Npc;

        EventManager.FireEvent("OnSelectEntity", CoreGame.SelectedEntity);
        EventManager.FireEvent("OnTalkToNpc", CoreGame.Player.State.DialogState.RequestedNpcId);
    }

    #endregion Methods

    #region Properties

    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB046;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    #endregion Properties
}






