using System.Collections.Generic;
using System;
using UBot.Core;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;

namespace UBot.Protocol.Legacy;

public static class LegacyGame
{
    public static GameClientType ClientType => Runtime.ClientType;
    public static bool Clientless => Runtime.Clientless;
    public static bool Ready { get => Runtime.Ready; set => Runtime.Ready = value; }
    public static Packet ChunkedPacket { get => Runtime.ChunkedPacket; set => Runtime.ChunkedPacket = value; }
    public static dynamic Player { get => Runtime.Player; set => Runtime.Player = value; }
    public static dynamic Party { get => Runtime.Party; set => Runtime.Party = value; }
    public static dynamic AcceptanceRequest { get => Runtime.AcceptanceRequest; set => Runtime.AcceptanceRequest = value; }
    public static dynamic SelectedEntity { get => Runtime.SelectedEntity; set => Runtime.SelectedEntity = value; }
    public static dynamic ReferenceManager => Runtime.ReferenceManager;

    private static IProtocolLegacyRuntime Runtime => UBot.Protocol.ProtocolRuntime.LegacyRuntime;
}

public static class LegacyKernel
{
    public static dynamic Proxy => UBot.Protocol.ProtocolRuntime.LegacyRuntime.Proxy;
    public static dynamic Bot => UBot.Protocol.ProtocolRuntime.LegacyRuntime.Bot;
    public static int TickCount => UBot.Protocol.ProtocolRuntime.LegacyRuntime.TickCount;
    public static bool EnableCollisionDetection => UBot.Protocol.ProtocolRuntime.LegacyRuntime.EnableCollisionDetection;
}

public static class Log
{
    public static void Debug(string message) => UBot.Protocol.ProtocolRuntime.LegacyRuntime.LogDebug(message);
    public static void Notify(string message) => UBot.Protocol.ProtocolRuntime.LegacyRuntime.LogNotify(message);
    public static void Warn(string message) => UBot.Protocol.ProtocolRuntime.LegacyRuntime.LogWarn(message);
    public static void Error(string message) => UBot.Protocol.ProtocolRuntime.LegacyRuntime.LogError(message);
    public static void Status(string message) => UBot.Protocol.ProtocolRuntime.LegacyRuntime.LogStatus(message);
    public static void StatusLang(string key, params object[] args) => UBot.Protocol.ProtocolRuntime.LegacyRuntime.LogStatusLang(key, args);
}

public static class LanguageManager
{
    public static string GetLangBySpecificKey(string parent, string key, string defaultValue = "")
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.GetLangBySpecificKey(parent, key, defaultValue);
}

public static class ClientManager
{
    public static void SetTitle(string title) => UBot.Protocol.ProtocolRuntime.LegacyRuntime.SetClientTitle(title);
}

public static class ProfileManager
{
    public static string SelectedCharacter
    {
        set => UBot.Protocol.ProtocolRuntime.LegacyRuntime.SetSelectedCharacter(value);
    }
}

public static class SpawnManager
{
    public static void Clear() => UBot.Protocol.ProtocolRuntime.LegacyRuntime.ClearSpawns();

    public static bool TryGetEntity<T>(uint uniqueId, out T entity)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.TryGetEntity(uniqueId, out entity);

    public static bool TryGetEntityIncludingMe<T>(uint uniqueId, out T entity)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.TryGetEntityIncludingMe(uniqueId, out entity);

    public static T GetEntity<T>(uint uniqueId)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.GetEntity<T>(uniqueId);

    public static T GetEntity<T>(Func<T, bool> predicate)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.GetEntity(predicate);
}

public static class ShoppingManager
{
    public static bool Running => UBot.Protocol.ProtocolRuntime.LegacyRuntime.ShoppingRunning;

    public static dynamic BuybackList
    {
        get => UBot.Protocol.ProtocolRuntime.LegacyRuntime.ShoppingBuybackList;
        set => UBot.Protocol.ProtocolRuntime.LegacyRuntime.ShoppingBuybackList = value;
    }
}

public static class ScriptManager
{
    public static bool Running => UBot.Protocol.ProtocolRuntime.LegacyRuntime.ScriptRunning;
}

public static class MoveScriptCommand
{
    public static bool MustDismount
    {
        get => UBot.Protocol.ProtocolRuntime.LegacyRuntime.MoveScriptMustDismount;
        set => UBot.Protocol.ProtocolRuntime.LegacyRuntime.MoveScriptMustDismount = value;
    }
}

public static class SkillManager
{
    public static List<SkillInfo> Buffs => UBot.Protocol.ProtocolRuntime.LegacyRuntime.SkillBuffs;
    public static void CastBuff(SkillInfo skill, uint target = 0, bool awaitBuffResponse = true)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.CastBuff(skill, target, awaitBuffResponse);
    public static bool CastSkill(SkillInfo skill, uint targetId = 0)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.CastSkill(skill, targetId);
    public static void CastSkillAt(SkillInfo skill, Position target)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.CastSkillAt(skill, target);
}

public static class AlchemyManager
{
    public static dynamic ActiveAlchemyItems => UBot.Protocol.ProtocolRuntime.LegacyRuntime.ActiveAlchemyItems;

    public static void BeginFuseRequest(object action, object type, object items)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.AlchemyBeginFuseRequest(action, type, items);

    public static void MarkError(object errorCode, object type)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.AlchemyMarkError(errorCode, type);

    public static void MarkCanceled(object type)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.AlchemyMarkCanceled(type);

    public static void MarkDestroyed(object oldItem, object type)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.AlchemyMarkDestroyed(oldItem, type);

    public static void MarkResult(bool isSuccess, object oldItem, object newItem, object type)
        => UBot.Protocol.ProtocolRuntime.LegacyRuntime.AlchemyMarkResult(isSuccess, oldItem, newItem, type);
}
