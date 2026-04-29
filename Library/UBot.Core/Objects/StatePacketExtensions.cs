using UBot.Core.Network;
using UBot.Core.Objects.Skill;

namespace UBot.Core.Objects;

internal static class StatePacketExtensions
{
    internal static void Deserialize(this State state, Packet packet)
    {
        state.LifeState = (LifeState)packet.ReadByte();

        if (state.LifeState == 0)
            state.LifeState = LifeState.Alive;

        if (Game.ClientType > GameClientType.Thailand)
            packet.ReadByte();

        state.MotionState = (MotionState)packet.ReadByte();
        state.BodyState = (BodyState)packet.ReadByte();

        if (Game.ClientType > GameClientType.Vietnam193)
            packet.ReadByte();

        state.WalkSpeed = packet.ReadFloat();
        state.RunSpeed = packet.ReadFloat();
        state.BerzerkSpeed = packet.ReadFloat();

        var buffCount = packet.ReadByte();
        for (var i = 0; i < buffCount; i++)
        {
            var id = packet.ReadUInt();
            var token = packet.ReadUInt();

            var buff = new SkillInfo(id, token);
            if (buff.Record == null)
                continue;

            if (buff.Record.Params.Contains(1701213281))
                packet.ReadBool();

            state.ActiveBuffs.Add(buff);
        }
    }
}
