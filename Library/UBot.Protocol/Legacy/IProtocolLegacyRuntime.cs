using System;
using System.Collections.Generic;
using UBot.Core;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;

namespace UBot.Protocol.Legacy;

public interface IProtocolLegacyRuntime
{
    GameClientType ClientType { get; }
    bool Clientless { get; }
    bool Ready { get; set; }
    Packet ChunkedPacket { get; set; }
    object Player { get; set; }
    object Party { get; set; }
    object AcceptanceRequest { get; set; }
    object SelectedEntity { get; set; }
    object ReferenceManager { get; }
    object Proxy { get; }
    object Bot { get; }
    bool ShoppingRunning { get; }
    object ShoppingBuybackList { get; set; }
    bool ScriptRunning { get; }
    bool MoveScriptMustDismount { get; set; }
    object ActiveAlchemyItems { get; }
    List<SkillInfo> SkillBuffs { get; }
    int TickCount { get; }
    bool EnableCollisionDetection { get; }

    void FireEvent(string eventName, params object[] args);
    void LogDebug(string message);
    void LogNotify(string message);
    void LogWarn(string message);
    void LogError(string message);
    void LogStatus(string message);
    void LogStatusLang(string key, params object[] args);
    string GetLangBySpecificKey(string parent, string key, string defaultValue = "");
    void SetClientTitle(string title);
    void LoadPlayerConfig(string characterName);
    object CreateNotificationSounds();
    void LoadNotificationSounds(object notificationSounds);
    void SetSelectedCharacter(string characterName);

    void ClearSpawns();
    bool TryGetEntity<T>(uint uniqueId, out T entity);
    bool TryGetEntityIncludingMe<T>(uint uniqueId, out T entity);
    T GetEntity<T>(uint uniqueId);
    T GetEntity<T>(Func<T, bool> predicate);
    void CastBuff(SkillInfo skill, uint target = 0, bool awaitBuffResponse = true);
    bool CastSkill(SkillInfo skill, uint targetId = 0);
    void CastSkillAt(SkillInfo skill, Position target);

    void AlchemyBeginFuseRequest(object action, object type, object items);
    void AlchemyMarkError(object errorCode, object type);
    void AlchemyMarkCanceled(object type);
    void AlchemyMarkDestroyed(object oldItem, object type);
    void AlchemyMarkResult(bool isSuccess, object oldItem, object newItem, object type);
}
