using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;
using UBot.Core.Objects.Spawn;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Action;

public class ActionBuffAddResponse : IPacketHandler
{
    public ushort Opcode => 0xB0BD;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
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
            ProtocolRuntime.EventBus?.Fire("OnAddBuff", buff);

            var buffName = buff.Record?.GetRealName() ?? buff.Id.ToString();
            ProtocolRuntime.Feedback?.Notify($"Buff [{buffName}] added.");
            return;
        }

        if (ProtocolRuntime.SpawnController?.GetEntity(targetId) is SpawnedBionic entity)
            entity.State.ActiveBuffs.Add(buff);
    }
}
