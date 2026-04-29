namespace UBot.Core.Abstractions.Services;

public interface IScriptRuntime
{
    string BasePath { get; }
    bool GameReady { get; }
    bool IsBotRunning { get; }
    bool PlayerInAction { get; }
    bool PlayerHasActiveVehicle { get; }
    bool PlayerIsInDungeon { get; }

    object PlayerPosition { get; }
    object PlayerMovementSource { get; }
    object FellowPosition { get; }

    object CreatePosition(byte xSector, byte ySector, float xOffset, float yOffset, float zOffset);
    double Distance(object source, object destination);
    double DistanceToPlayer(object position);
    bool HasCollisionBetween(object source, object destination);
    bool MovePlayerTo(object destination);
    int EstimateMoveDelayMilliseconds(object source, object destination);

    bool GetConfigBool(string key, bool defaultValue);
    string GetConfigString(string key, string defaultValue);

    bool HasActiveSpeedBuff();
    bool UseSpeedDrug();
    void SummonFellow();
    void CastFellowSkill(string codeName);
    void MountFellow();
    void SummonVehicle();
    void DismountVehicle();

    bool Teleport(string npcCodeName, uint destination);
    object GetPlayerSkillByCodeName(string codeName);
    void CastBuff(object skill);

    void FireEvent(string eventName, params object[] args);
}
