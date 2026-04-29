using System;

namespace UBot.Core.Abstractions;

public interface IGameStateRuntimeContext
{
    GameClientType ClientType { get; }
    IReferenceManager References { get; }
    int TickCount { get; }
    bool IsBotRunning { get; }
    bool IsPlayerInAction { get; }
    string PlayerName { get; }
    uint PlayerUniqueId { get; }
    int PlayerLevel { get; }
    object Player { get; }
    object SelectedEntity { get; }
    object AcceptanceRequest { get; }

    object GetReference(string kind, object key);
    object GetEntity(Type entityType, object key);
    object GetEntities(Type entityType, Func<object, bool> predicate);

    bool SendPlayerMove(object destination, bool sleep);
    bool SendEnterBerzerkMode();
    bool SendSelectEntity(uint uniqueId);
    bool SendDeselectEntity(uint uniqueId);
    bool SendPartyInvite(uint playerUniqueId, bool isInParty, byte partyType);
    void SendPartyLeave();
    bool SendInventoryMove(byte sourceSlot, byte destinationSlot, ushort amount);
    bool SendStorageMove(byte sourceSlot, byte destinationSlot, ushort amount, object npc);
    bool SendUseInventoryItem(byte slot, int tid);
    bool SendUseInventoryItemTo(byte slot, int tid, byte destinationSlot, int mapId);
    void SendUseInventoryItemFor(byte slot, int tid, uint uniqueId);
    bool SendDropInventoryItem(byte slot, bool cos, uint? cosUniqueId);

    bool HasPendingPartyRequest();
    bool IsSelectedEntityNpc();
    bool IsBehindObstacle(object position);
    double DistanceToPlayer(object position);
    bool StopBot();
    bool GetConfigBool(string key);
    void FireEvent(string eventName);
    void LogDebug(string message);
    void LogNotify(string message);
}

public static class GameStateRuntimeProvider
{
    public static IGameStateRuntimeContext Instance { get; set; }
}
