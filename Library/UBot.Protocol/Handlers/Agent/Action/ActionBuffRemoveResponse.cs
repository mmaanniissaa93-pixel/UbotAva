using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;
using UBot.Core.Objects.Spawn;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Action;

public class ActionBuffRemoveResponse : IPacketHandler
{
    public ushort Opcode => 0xB072;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        var buffTokensCount = packet.ReadByte();

        for (var i = 0; i < buffTokensCount; i++)
        {
            var token = packet.ReadUInt();
            if (token == 0)
                continue;

            if (player.State.TryRemoveActiveBuff(token, out SkillInfo buff))
            {
                ProtocolRuntime.Feedback?.Notify($"The buff [{buff.Record?.GetRealName()}] expired");
                ProtocolRuntime.EventBus?.Fire("OnRemoveBuff", buff);

                var playerSkill = player.Skills.GetSkillInfoById(buff.Id);
                playerSkill?.Reset();
                continue;
            }

            if (ProtocolRuntime.SpawnController?.FindEntity(entity =>
                    entity is SpawnedBionic bionic && bionic.State.TryGetActiveBuff(token, out _)
                ) is SpawnedBionic target)
            {
                target.State.TryRemoveActiveBuff(token, out _);
            }
            else
            {
                ProtocolRuntime.Feedback?.Warn($"{token} not found while trying remove buff with token!");
            }
        }
    }
}
