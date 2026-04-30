using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Inventory;
using UBot.Core.Objects.Spawn;

namespace UBot.AutoDungeon;

internal sealed record MonsterCountEntry(string Name, MonsterRarity Rarity);

internal static class AutoDungeonState
{
    private static readonly object _lock = new();
    private static readonly TimeSpan DimensionalActiveWindow = TimeSpan.FromHours(2);
    private static readonly TimeSpan PendingUseWindow = TimeSpan.FromSeconds(15);

    private static HashSet<string> _ignoreNames = new(StringComparer.OrdinalIgnoreCase);
    private static HashSet<MonsterRarity> _ignoreTypes = [];
    private static HashSet<MonsterRarity> _onlyCountTypes = [];

    private static bool _acceptForgottenWorld;

    private static string _pendingDimensionalItemName;
    private static DateTime _pendingDimensionalExpirationUtc;

    private static string _activeDimensionalItemName;
    private static DateTime _activeDimensionalExpirationUtc;

    public static IReadOnlyCollection<string> IgnoreNames
    {
        get
        {
            lock (_lock)
                return _ignoreNames.ToArray();
        }
    }

    public static IReadOnlyCollection<MonsterRarity> IgnoreTypes
    {
        get
        {
            lock (_lock)
                return _ignoreTypes.ToArray();
        }
    }

    public static IReadOnlyCollection<MonsterRarity> OnlyCountTypes
    {
        get
        {
            lock (_lock)
                return _onlyCountTypes.ToArray();
        }
    }

    public static bool AcceptForgottenWorld
    {
        get
        {
            lock (_lock)
                return _acceptForgottenWorld;
        }
        set
        {
            lock (_lock)
                _acceptForgottenWorld = value;

            AutoDungeonConfig.SaveAcceptForgottenWorld(value);
        }
    }

    public static void LoadFromConfig()
    {
        lock (_lock)
        {
            _ignoreNames = AutoDungeonConfig.LoadIgnoreNames();
            _ignoreTypes = AutoDungeonConfig.LoadIgnoreTypes();
            _onlyCountTypes = AutoDungeonConfig.LoadOnlyCountTypes();
            _acceptForgottenWorld = AutoDungeonConfig.LoadAcceptForgottenWorld();
        }
    }

    public static void SetIgnoreNames(IEnumerable<string> names)
    {
        var values = names
            ?.Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        lock (_lock)
            _ignoreNames = values;

        AutoDungeonConfig.SaveIgnoreNames(values);
    }

    public static void SetIgnoreTypes(IEnumerable<MonsterRarity> types)
    {
        var values = types?.Distinct().ToHashSet() ?? [];

        lock (_lock)
            _ignoreTypes = values;

        AutoDungeonConfig.SaveIgnoreTypes(values);
    }

    public static void SetOnlyCountTypes(IEnumerable<MonsterRarity> types)
    {
        var values = types?.Distinct().ToHashSet() ?? [];

        lock (_lock)
            _onlyCountTypes = values;

        AutoDungeonConfig.SaveOnlyCountTypes(values);
    }

    public static void ClearRuntimeState()
    {
        lock (_lock)
        {
            _pendingDimensionalItemName = null;
            _pendingDimensionalExpirationUtc = DateTime.MinValue;
            _activeDimensionalItemName = null;
            _activeDimensionalExpirationUtc = DateTime.MinValue;
        }
    }

    public static List<MonsterCountEntry> GetMonsterCounterSnapshot(Position centerPosition, double? radius = null)
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready || UBot.Core.RuntimeAccess.Session.Player == null)
            return [];

        if (!SpawnManager.TryGetEntities<SpawnedMonster>(out var monsters))
            return [];

        HashSet<string> ignoreNames;
        HashSet<MonsterRarity> ignoreTypes;
        HashSet<MonsterRarity> onlyCountTypes;

        lock (_lock)
        {
            ignoreNames = [.. _ignoreNames];
            ignoreTypes = [.. _ignoreTypes];
            onlyCountTypes = [.. _onlyCountTypes];
        }

        var result = new List<MonsterCountEntry>();

        foreach (var monster in monsters)
        {
            if (!ShouldCountMonster(monster, centerPosition, radius, ignoreNames, ignoreTypes, onlyCountTypes))
                continue;

            result.Add(new MonsterCountEntry(monster.Record.GetRealName(), monster.Rarity));
        }

        return result
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => (byte)entry.Rarity)
            .ToList();
    }

    public static bool WaitUntilAreaCleared(
        Position centerPosition,
        double? radius,
        int timeoutSeconds,
        Func<bool> continueCondition
    )
    {
        var timeout = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        var consecutiveNoMobChecks = 0;

        while (DateTime.UtcNow < timeout)
        {
            if (continueCondition != null && !continueCondition())
                return false;

            var currentCount = GetMonsterCounterSnapshot(centerPosition, radius).Count;
            if (currentCount == 0)
            {
                consecutiveNoMobChecks++;
                if (consecutiveNoMobChecks >= 2)
                    return true;
            }
            else
            {
                consecutiveNoMobChecks = 0;
            }

            Thread.Sleep(1000);
        }

        Log.Warn("[AutoDungeon] Timeout while waiting for the area to be cleared.");
        return false;
    }

    public static void WaitForPotentialDrops(Position centerPosition, int radius, int maxSeconds)
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var playerJid = UBot.Core.RuntimeAccess.Session.Player.JID;

        for (var waited = 0; waited < maxSeconds; waited++)
        {
            if (
                !SpawnManager.TryGetEntities<SpawnedItem>(
                    item =>
                        item.Movement.Source.DistanceTo(centerPosition) <= radius + 15
                        && (!item.HasOwner || item.OwnerJID == playerJid),
                    out _
                )
            )
            {
                return;
            }

            Thread.Sleep(1000);
        }

        Log.Warn("[AutoDungeon] Drop pickup wait timeout reached.");
    }

    public static bool StartGoDimensionalFlow(string preferredItemName)
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready || UBot.Core.RuntimeAccess.Session.Player?.Inventory == null)
            return false;

        if (TryEnterAlreadyOpenDimensional(preferredItemName))
            return true;

        var dimensionalItem = FindDimensionalItem(preferredItemName);
        if (dimensionalItem == null)
        {
            Log.Warn(
                string.IsNullOrWhiteSpace(preferredItemName)
                    ? "[AutoDungeon] Could not find a dimensional hole item in inventory."
                    : $"[AutoDungeon] Could not find dimensional item '{preferredItemName}'."
            );
            return false;
        }

        var itemName = dimensionalItem.Record.GetRealName();

        lock (_lock)
        {
            _pendingDimensionalItemName = itemName;
            _pendingDimensionalExpirationUtc = DateTime.UtcNow.Add(PendingUseWindow);
        }

        Log.Notify($"[AutoDungeon] Using dimensional item '{itemName}'...");

        if (dimensionalItem.Use())
            return true;

        lock (_lock)
        {
            _pendingDimensionalItemName = null;
            _pendingDimensionalExpirationUtc = DateTime.MinValue;
        }

        Log.Warn($"[AutoDungeon] Failed to use dimensional item '{itemName}'.");
        return false;
    }

    public static void OnDimensionalItemUseResponse(bool success)
    {
        string itemName;

        lock (_lock)
        {
            if (DateTime.UtcNow > _pendingDimensionalExpirationUtc || string.IsNullOrWhiteSpace(_pendingDimensionalItemName))
                return;

            itemName = _pendingDimensionalItemName;
            _pendingDimensionalItemName = null;
            _pendingDimensionalExpirationUtc = DateTime.MinValue;
        }

        if (!success)
        {
            Log.Warn($"[AutoDungeon] Dimensional item '{itemName}' could not be opened.");
            return;
        }

        lock (_lock)
        {
            _activeDimensionalItemName = itemName;
            _activeDimensionalExpirationUtc = DateTime.UtcNow.Add(DimensionalActiveWindow);
        }

        Log.Notify($"[AutoDungeon] Dimensional item '{itemName}' opened. Entering portal...");

        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            EnterDimensionalPortal(itemName);
        });
    }

    public static bool TryBuildForgottenWorldAcceptPacket(Packet requestPacket, out Packet response)
    {
        response = null;

        if (!AcceptForgottenWorld)
            return false;

        if (requestPacket == null || requestPacket.Remaining < 4)
            return false;

        var requestId = requestPacket.ReadUInt();

        response = new Packet(0x751C);
        response.WriteUInt(requestId);
        response.WriteUInt(0);
        response.WriteByte(1);

        return true;
    }

    private static bool TryEnterAlreadyOpenDimensional(string preferredItemName)
    {
        string activeName;

        lock (_lock)
        {
            if (DateTime.UtcNow > _activeDimensionalExpirationUtc || string.IsNullOrWhiteSpace(_activeDimensionalItemName))
                return false;

            activeName = _activeDimensionalItemName;
        }

        if (!string.IsNullOrWhiteSpace(preferredItemName) && !activeName.Equals(preferredItemName, StringComparison.OrdinalIgnoreCase))
            return false;

        Log.Notify($"[AutoDungeon] Dimensional item '{activeName}' is already active. Entering portal...");
        return EnterDimensionalPortal(activeName);
    }

    private static InventoryItem FindDimensionalItem(string preferredItemName)
    {
        if (UBot.Core.RuntimeAccess.Session.Player?.Inventory == null)
            return null;

        if (!string.IsNullOrWhiteSpace(preferredItemName))
        {
            var preferredName = preferredItemName.Trim();
            var exactMatch = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItem(item =>
                item.Record.GetRealName().Equals(preferredName, StringComparison.OrdinalIgnoreCase)
                || item.Record.CodeName.Equals(preferredName, StringComparison.OrdinalIgnoreCase)
            );

            if (exactMatch != null)
                return exactMatch;
        }

        return UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItem(item => IsDimensionalItem(item.Record));
    }

    private static bool IsDimensionalItem(UBot.GameData.ReferenceObjects.RefObjItem record)
    {
        if (record == null)
            return false;

        if (record.TypeID1 == 3 && record.TypeID2 == 12 && record.TypeID3 == 7)
            return true;

        return record.CodeName.Contains("DIMENSION", StringComparison.OrdinalIgnoreCase)
            || record.CodeName.Contains("FORGOTTEN", StringComparison.OrdinalIgnoreCase)
            || record.CodeName.Contains("DUNGEON", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EnterDimensionalPortal(string itemName)
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready || UBot.Core.RuntimeAccess.Session.Player == null)
            return false;

        if (!TryFindDimensionalPortal(out var portal))
        {
            Log.Warn("[AutoDungeon] Could not find an opened dimensional portal nearby.");
            return false;
        }

        portal.TrySelect();
        Thread.Sleep(350);
        portal.TryDeselect();

        var packet = new Packet(0x705A);
        packet.WriteUInt(portal.UniqueId);
        packet.WriteByte((byte)TeleportType.RUNTIME_PORTAL);
        packet.WriteByte(0);

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);

        Log.Notify("[AutoDungeon] Entered dimensional portal.");
        return true;
    }

    private static bool TryFindDimensionalPortal(out SpawnedPortal portal)
    {
        portal = null;

        if (!SpawnManager.TryGetEntities<SpawnedPortal>(out var portals) || !portals.Any())
            return false;

        var player = UBot.Core.RuntimeAccess.Session.Player;
        var candidates = portals.Where(p => p != null).ToList();

        var ownedPortal = candidates
            .Where(p => p.OwnerUniqueId == player.UniqueId || p.OwnerName == player.Name)
            .OrderBy(p => p.DistanceToPlayer)
            .FirstOrDefault();

        if (ownedPortal != null)
        {
            portal = ownedPortal;
            return true;
        }

        var dimensionLikePortal = candidates
            .Where(p => p.OwnerUniqueId != 0 || !string.IsNullOrWhiteSpace(p.OwnerName))
            .OrderBy(p => p.DistanceToPlayer)
            .FirstOrDefault();

        if (dimensionLikePortal != null)
        {
            portal = dimensionLikePortal;
            return true;
        }

        portal = candidates.OrderBy(p => p.DistanceToPlayer).FirstOrDefault();
        return portal != null;
    }

    private static bool ShouldCountMonster(
        SpawnedMonster monster,
        Position centerPosition,
        double? radius,
        HashSet<string> ignoreNames,
        HashSet<MonsterRarity> ignoreTypes,
        HashSet<MonsterRarity> onlyCountTypes
    )
    {
        if (monster?.Record == null)
            return false;

        if (monster.State.LifeState == LifeState.Dead)
            return false;

        if (radius.HasValue && radius.Value > 0)
        {
            var distanceToCenter = centerPosition.DistanceTo(monster.Movement.Source);
            if (distanceToCenter > radius.Value)
                return false;
        }

        if (ignoreTypes.Contains(monster.Rarity))
            return false;

        if (onlyCountTypes.Count > 0)
            return onlyCountTypes.Contains(monster.Rarity);

        return !ignoreNames.Contains(monster.Record.GetRealName());
    }

    public static void SetTrainingArea(Position center, int radius)
    {
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Region", center.Region);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.X", center.XOffset);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Y", center.YOffset);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Z", center.ZOffset);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Radius", radius);

        UBot.Core.RuntimeAccess.Events.FireEvent("OnSetTrainingArea");
    }
}
