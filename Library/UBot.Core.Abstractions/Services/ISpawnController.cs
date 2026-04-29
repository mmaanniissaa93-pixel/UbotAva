namespace UBot.Core.Abstractions.Services;

public interface ISpawnController
{
    void Parse(object packet, bool isGroup = false);
    void BeginGroup(byte type, ushort amount);
    void AppendGroupData(byte[] packetBytes);
    void EndGroup();
    void Despawn(uint uniqueId);
}
