using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UBot.Core.Abstractions;
using UBot.Core.Client.ReferenceObjects;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreProtocolLegacyRuntime : UBot.Protocol.Legacy.IProtocolLegacyRuntime
{
    public GameClientType ClientType => Game.ClientType;
    public bool Clientless => Game.Clientless;
    public bool Ready { get => Game.Ready; set => Game.Ready = value; }
    public Packet ChunkedPacket { get => Game.ChunkedPacket; set => Game.ChunkedPacket = value; }
    public object Player { get => Game.Player; set => Game.Player = (Objects.Player)value; }
    public object Party { get => Game.Party; set => Game.Party = (Objects.Party.Party)value; }
    public object AcceptanceRequest { get => Game.AcceptanceRequest; set => Game.AcceptanceRequest = (AcceptanceRequest)value; }
    public object SelectedEntity { get => Game.SelectedEntity; set => Game.SelectedEntity = (Objects.Spawn.SpawnedBionic)value; }
    public object ReferenceManager => Game.ReferenceManager;
    public object Proxy => Kernel.Proxy;
    public object Bot => Kernel.Bot;
    public bool ShoppingRunning => ShoppingManager.Running;
    public object ShoppingBuybackList { get => ShoppingManager.BuybackList; set => ShoppingManager.BuybackList = (Dictionary<byte, InventoryItem>)value; }
    public bool ScriptRunning => ScriptManager.Running;
    public bool MoveScriptMustDismount
    {
        get => Components.Scripting.Commands.MoveScriptCommand.MustDismount;
        set => Components.Scripting.Commands.MoveScriptCommand.MustDismount = value;
    }
    public object ActiveAlchemyItems => AlchemyManager.ActiveAlchemyItems;
    public List<SkillInfo> SkillBuffs => SkillManager.Buffs;
    public int TickCount => Kernel.TickCount;
    public bool EnableCollisionDetection => Kernel.EnableCollisionDetection;

    public void FireEvent(string eventName, params object[] args) => EventManager.FireEvent(eventName, args);
    public void LogDebug(string message) => Log.Debug(message);
    public void LogNotify(string message) => Log.Notify(message);
    public void LogWarn(string message) => Log.Warn(message);
    public void LogError(string message) => Log.Error(message);
    public void LogStatus(string message) => Log.Status(message);
    public void LogStatusLang(string key, params object[] args) => Log.StatusLang(key, args);
    public string GetLangBySpecificKey(string parent, string key, string defaultValue = "")
        => LanguageManager.GetLangBySpecificKey(parent, key, defaultValue);
    public void SetClientTitle(string title) => ClientManager.SetTitle(title);
    public void LoadPlayerConfig(string characterName) => PlayerConfig.Load(characterName);
    public object CreateNotificationSounds() => new Objects.NotificationSounds();
    public void LoadNotificationSounds(object notificationSounds)
        => ((Objects.NotificationSounds)notificationSounds).LoadPlayerSettings();
    public void SetSelectedCharacter(string characterName) => ProfileManager.SelectedCharacter = characterName;
    public void ClearSpawns() => SpawnManager.Clear();

    public bool TryGetEntity<T>(uint uniqueId, out T entity)
    {
        entity = default;
        var result = InvokeSpawnGeneric(nameof(SpawnManager.TryGetEntity), typeof(T), uniqueId);
        if (result.Success)
            entity = (T)result.Entity;
        return result.Success;
    }

    public bool TryGetEntityIncludingMe<T>(uint uniqueId, out T entity)
    {
        entity = default;
        var result = InvokeSpawnGeneric(nameof(SpawnManager.TryGetEntityIncludingMe), typeof(T), uniqueId);
        if (result.Success)
            entity = (T)result.Entity;
        return result.Success;
    }

    public T GetEntity<T>(uint uniqueId)
    {
        var method = typeof(SpawnManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(method => method.Name == nameof(SpawnManager.GetEntity)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 1);

        return (T)method.MakeGenericMethod(typeof(T)).Invoke(null, new object[] { uniqueId });
    }

    public T GetEntity<T>(Func<T, bool> predicate)
    {
        var method = typeof(SpawnManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(method => method.Name == nameof(SpawnManager.GetEntity)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 1
                && method.GetParameters()[0].ParameterType.Name.StartsWith("Func", StringComparison.Ordinal));

        return (T)method.MakeGenericMethod(typeof(T)).Invoke(null, new object[] { predicate });
    }

    public void CastBuff(SkillInfo skill, uint target = 0, bool awaitBuffResponse = true)
        => SkillManager.CastBuff(skill, target, awaitBuffResponse);

    public bool CastSkill(SkillInfo skill, uint targetId = 0)
        => SkillManager.CastSkill(skill, targetId);

    public void CastSkillAt(SkillInfo skill, Position target)
        => SkillManager.CastSkillAt(skill, target);

    public void AlchemyBeginFuseRequest(object action, object type, object items)
        => AlchemyManager.BeginFuseRequest((AlchemyAction)action, (AlchemyType)type, (List<InventoryItem>)items);

    public void AlchemyMarkError(object errorCode, object type)
        => AlchemyManager.MarkError((ushort)errorCode, (AlchemyType)type);

    public void AlchemyMarkCanceled(object type)
        => AlchemyManager.MarkCanceled((AlchemyType)type);

    public void AlchemyMarkDestroyed(object oldItem, object type)
        => AlchemyManager.MarkDestroyed((InventoryItem)oldItem, (AlchemyType)type);

    public void AlchemyMarkResult(bool isSuccess, object oldItem, object newItem, object type)
        => AlchemyManager.MarkResult(isSuccess, (InventoryItem)oldItem, (InventoryItem)newItem, (AlchemyType)type);

    private static (bool Success, object Entity) InvokeSpawnGeneric(string methodName, Type entityType, uint uniqueId)
    {
        var method = typeof(SpawnManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(method => method.Name == methodName
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 2
                && method.GetParameters()[0].ParameterType == typeof(uint));

        var args = new object[] { uniqueId, null };
        var success = (bool)method.MakeGenericMethod(entityType).Invoke(null, args);
        return (success, args[1]);
    }
}
