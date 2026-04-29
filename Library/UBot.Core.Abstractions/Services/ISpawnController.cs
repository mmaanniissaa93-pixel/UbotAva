namespace UBot.Core.Abstractions.Services;

public interface ISpawnController
{
    object GetEntity(uint uniqueId);
    object FindEntity(System.Func<object, bool> predicate);
    void Parse(object packet, bool isGroup = false);
    void BeginGroup(byte type, ushort amount);
    void AppendGroupData(byte[] packetBytes);
    void EndGroup();
    void Despawn(uint uniqueId);
}
