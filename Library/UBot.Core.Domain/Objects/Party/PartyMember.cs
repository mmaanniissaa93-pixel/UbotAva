using UBot.Core.Abstractions;

namespace UBot.Core.Objects.Party;

public class PartyMember
{
    public string Guild;
    public byte HealthMana;
    public byte Level;
    public uint MasteryId1;
    public uint MasteryId2;
    public uint MemberId;
    public string Name;
    public uint ObjectId;
    public Position Position;

    public dynamic Player => RuntimeContext?.GetSpawnedPlayer(Name);

    public dynamic Record => ReferenceProvider.Instance?.GetRefObjChar(ObjectId);

    public void Banish()
    {
        RuntimeContext?.Banish(this);
    }

    public static IPartyMemberRuntimeContext RuntimeContext { get; set; }
}

public interface IPartyMemberRuntimeContext
{
    object GetSpawnedPlayer(string name);
    void Banish(PartyMember member);
}
