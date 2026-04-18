using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects.Skill;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.Network.Handler.Agent.Action;

internal class ActionBuffAddResponse : IPacketHandler
{
    /// <summary>
    ///     Invokes the specified packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        var player = Game.Player;
        if (player == null)
            return;

        var targetId = packet.ReadUInt();
        var skillId = packet.ReadUInt();
        var token = packet.ReadUInt();
        if (token == 0)
            return;

        var buff = new SkillInfo(skillId, token);
        if (targetId == player.UniqueId)
        {
            var playerBuff = player.Skills.GetSkillInfoById(buff.Id);
            if (playerBuff != null)
            {
                buff = playerBuff;
                playerBuff.Token = token;
                playerBuff.Update();
            }
            else
            {
                buff.Update();
            }

            player.State.ActiveBuffs.Add(buff);

            EventManager.FireEvent("OnAddBuff", buff);

            var buffName = buff.Record?.GetRealName() ?? buff.Id.ToString();
            Log.Notify($"Buff [{buffName}] added.");

            return;
        }

        if (SpawnManager.TryGetEntity<SpawnedBionic>(targetId, out var entity))
            entity.State.ActiveBuffs.Add(buff);
    }

    #region Properites

    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB0BD;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    #endregion Properites
}
