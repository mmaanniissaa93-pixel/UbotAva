using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UBot.FileSystem;
using UBot.NavMeshApi;
using UBot.NavMeshApi.Dungeon;
using UBot.NavMeshApi.Edges;
using UBot.NavMeshApi.Extensions;
using UBot.NavMeshApi.Terrain;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Network.Protocol;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Core.Objects.Skill;
using UBot.Core.Plugins;
using Forms = System.Windows.Forms;
using CoreRegion = UBot.Core.Objects.Region;

namespace UBot.Avalonia.Services;

internal sealed class UbotActionRouter : UbotServiceBase
{
    private readonly UbotGeneralActionHandler _generalActionHandler;
    private readonly UbotInventoryActionHandler _inventoryActionHandler;
    private readonly UbotPartyActionHandler _partyActionHandler;

    internal UbotActionRouter(
        UbotGeneralActionHandler generalActionHandler,
        UbotInventoryActionHandler inventoryActionHandler,
        UbotPartyActionHandler partyActionHandler)
    {
        _generalActionHandler = generalActionHandler;
        _inventoryActionHandler = inventoryActionHandler;
        _partyActionHandler = partyActionHandler;
    }

    internal async Task<bool> InvokePluginActionAsync(string pluginId, string action, Dictionary<string, object?>? payload = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            return false;

        payload ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var normalized = action.Trim().ToLowerInvariant();

        if (normalized.StartsWith("inventory."))
            return _inventoryActionHandler.HandleInventoryAction(normalized, payload);

        if (normalized.StartsWith("party."))
            return _partyActionHandler.HandlePartyAction(normalized, payload);

        return await _generalActionHandler.HandleActionAsync(normalized, payload).ConfigureAwait(false);
    }
}

internal sealed class UbotGeneralActionHandler : UbotServiceBase
{
    private readonly UbotConnectionService _connectionService;
    private readonly UbotMapService _mapService;

    internal UbotGeneralActionHandler(UbotConnectionService connectionService, UbotMapService mapService)
    {
        _connectionService = connectionService;
        _mapService = mapService;
    }

    internal async Task<bool> HandleActionAsync(string normalized, Dictionary<string, object?> payload)
    {
        if (normalized == "general.start-client")
            return await _connectionService.StartClientAsync().ConfigureAwait(false);
        if (normalized == "general.start-clientless")
            return await _connectionService.GoClientlessAsync().ConfigureAwait(false);
        if (normalized == "core.disconnect")
        {
            await _connectionService.DisconnectAsync().ConfigureAwait(false);
            return true;
        }
        if (normalized == "core.save-config")
        {
            await _connectionService.SaveConfigAsync().ConfigureAwait(false);
            return true;
        }

        if (normalized is "general.toggle-client-visibility")
            return await _connectionService.ToggleClientVisibilityAsync().ConfigureAwait(false);

        if (normalized is "general.toggle-pending-window")
            return TogglePendingWindow();

        if (normalized is "general.open-account-setup" or "general.open-accounts-window")
            return OpenAccountsWindow();

        if (normalized is "general.open-sound-settings")
        {
            UBot.Core.RuntimeAccess.Events.FireEvent("OnOpenSoundSettings");
            return true;
        }

        if (normalized is "protection.apply-stat-points")
        {
            UBot.Core.RuntimeAccess.Events.FireEvent("OnApplyStatPoints");
            return true;
        }

        if (normalized is "statistics.reset")
        {
            UBot.Core.RuntimeAccess.Events.FireEvent("OnResetStatistics");
            return true;
        }

        if (normalized is "chat.send")
            return HandleChatSendAction(payload);

        if (normalized is "log.clear")
            return true;

        if (normalized is "map.walk-to" or "map.walk" or "map.goto")
            return HandleMapWalkToAction(payload);

        if (normalized is "map.reset-to-player" or "map.center-on-player" or "map.navmesh.reset-to-player")
        {
            UBot.Core.RuntimeAccess.Player.Set("UBot.Desktop.Map.ResetToPlayerAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            UBot.Core.RuntimeAccess.Player.Save();
            return true;
        }

        if (normalized is "training.set-area-current" or "training.use-current-position")
            return HandleSetTrainingAreaToCurrentPosition();

        return false;
    }

    private bool HandleSetTrainingAreaToCurrentPosition()
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return false;

        var p = UBot.Core.RuntimeAccess.Session.Player.Position;
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Region", (ushort)p.Region);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.X", p.XOffset);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Y", p.YOffset);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Z", p.ZOffset);
        UBot.Core.RuntimeAccess.Events.FireEvent("OnSetTrainingArea");
        UBot.Core.RuntimeAccess.Player.Save();
        return true;
    }

    private bool HandleMapWalkToAction(Dictionary<string, object?> payload)
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return false;

        if (!TryGetDoubleValue(payload, "mapX", out var mapX))
            return false;
        if (!TryGetDoubleValue(payload, "mapY", out var mapY))
            return false;

        var source = UBot.Core.RuntimeAccess.Session.Player.Position;
        if (!_mapService.TryResolveWalkDestination(source, mapX, mapY, out var destination))
            return false;

        var movementTarget = ClampMapWalkStep(source, destination, MapClickMaxStepDistance);
        return TrySendPlayerMovePacket(movementTarget);
    }

    private static Position ClampMapWalkStep(Position source, Position destination, double maxDistance)
    {
        var distance = source.DistanceTo(destination);
        if (distance <= maxDistance || distance <= 0.01)
            return destination;

        var ratio = (float)(maxDistance / distance);
        var worldX = source.X + (destination.X - source.X) * ratio;
        var worldY = source.Y + (destination.Y - source.Y) * ratio;
        return new Position(worldX, worldY, source.Region) { ZOffset = source.ZOffset };
    }

    private static bool TrySendPlayerMovePacket(Position destination)
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return false;

        var packet = new Packet(0x7021);
        packet.WriteByte(1);

        if (!UBot.Core.RuntimeAccess.Session.Player.IsInDungeon)
        {
            packet.WriteUShort(destination.Region);
            packet.WriteShort((short)Math.Clamp(Math.Round(destination.XOffset), short.MinValue, short.MaxValue));
            packet.WriteShort((short)Math.Clamp(Math.Round(destination.ZOffset), short.MinValue, short.MaxValue));
            packet.WriteShort((short)Math.Clamp(Math.Round(destination.YOffset), short.MinValue, short.MaxValue));
        }
        else
        {
            packet.WriteUShort(UBot.Core.RuntimeAccess.Session.Player.Position.Region);
            packet.WriteInt((int)Math.Round(destination.XOffset));
            packet.WriteInt((int)Math.Round(destination.ZOffset));
            packet.WriteInt((int)Math.Round(destination.YOffset));
        }

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
        return true;
    }

    private static bool HandleChatSendAction(Dictionary<string, object?> payload)
    {
        if (!TryGetStringValue(payload, "message", out var message) || string.IsNullOrWhiteSpace(message))
            return false;

        var channel = TryGetStringValue(payload, "channel", out var parsedChannel)
            ? parsedChannel.Trim().ToLowerInvariant()
            : "all";

        if (channel == "global")
        {
            SendGlobalChatPacket(message.Trim());
            return true;
        }

        var chatType = channel switch
        {
            "private" => ChatType.Private,
            "party" => ChatType.Party,
            "guild" => ChatType.Guild,
            "union" => ChatType.Union,
            "academy" => ChatType.Academy,
            "stall" => ChatType.Stall,
            _ => ChatType.All
        };

        string? receiver = null;
        if (chatType == ChatType.Private)
        {
            if (!TryGetStringValue(payload, "target", out var target))
                return false;
            receiver = target?.Trim();
            if (string.IsNullOrWhiteSpace(receiver))
                return false;
        }

        SendChatPacket(chatType, message.Trim(), receiver);
        return true;
    }

    private static void SendChatPacket(ChatType type, string message, string? receiver = null)
    {
        var packet = new Packet(0x7025);
        packet.WriteByte(type);
        packet.WriteByte(1);

        if (UBot.Core.RuntimeAccess.Session.ClientType > GameClientType.Vietnam)
            packet.WriteByte(0);
        if (UBot.Core.RuntimeAccess.Session.ClientType >= GameClientType.Chinese_Old)
            packet.WriteByte(0);

        if (type == ChatType.Private)
            packet.WriteString(receiver ?? string.Empty);

        packet.WriteConditonalString(message);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private static void SendGlobalChatPacket(string message)
    {
        var item = UBot.Core.RuntimeAccess.Session.Player?.Inventory?.GetItem(new TypeIdFilter(3, 3, 3, 5));
        if (item == null)
            return;

        var packet = new Packet(0x704C);
        packet.WriteByte(item.Slot);
        if (UBot.Core.RuntimeAccess.Session.ClientType > GameClientType.Vietnam)
        {
            packet.WriteInt(item.Record.Tid);
            packet.WriteByte(0);
        }
        else
        {
            packet.WriteUShort((ushort)item.Record.Tid);
        }

        packet.WriteConditonalString(message);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private static bool TogglePendingWindow()
    {
        try
        {
            var pendingWindowType = Type.GetType("UBot.General.Views.PendingWindow, UBot.General", false);
            if (pendingWindowType == null)
                return false;

            var instanceProperty = pendingWindowType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var form = instanceProperty?.GetValue(null) as Forms.Form;
            if (form == null)
                return false;

            if (form.Visible)
                form.Hide();
            else
                form.Show();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool OpenAccountsWindow()
    {
        try
        {
            var viewType = Type.GetType("UBot.General.Views.View, UBot.General", false);
            if (viewType == null)
                return false;

            var accountsWindowProperty = viewType.GetProperty("AccountsWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var mainViewProperty = viewType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var form = accountsWindowProperty?.GetValue(null) as Forms.Form;
            if (form == null)
                return false;

            if (form.Visible)
            {
                form.BringToFront();
                form.Focus();
                return true;
            }

            var owner = mainViewProperty?.GetValue(null) as Forms.Control;
            if (owner != null)
                form.Show(owner);
            else
                form.Show();

            form.BringToFront();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class UbotInventoryActionHandler : UbotServiceBase
{
    internal bool HandleInventoryAction(string action, Dictionary<string, object?> payload)
    {
        if (action == "inventory.set-type")
        {
            if (TryGetStringValue(payload, "type", out var type))
            {
                UBot.Core.RuntimeAccess.Player.Set("UBot.Desktop.Inventory.SelectedTab", type);
                UBot.Core.RuntimeAccess.Player.Save();
                return true;
            }
        }

        if (action == "inventory.use")
        {
            if (TryResolveActionItem(payload, out var item))
                return item.Use();
        }

        if (action == "inventory.drop")
        {
            if (TryResolveActionItem(payload, out var item))
                return item.Drop();
        }

        if (action == "inventory.use-reverse")
        {
            if (TryResolveActionItem(payload, out var item) && item.Equals(ReverseReturnScrollFilter))
            {
                if (TryGetIntValue(payload, "mode", out var mode))
                {
                    if (mode == 7)
                    {
                        if (TryGetIntValue(payload, "mapId", out var mapId))
                            return item.UseTo((byte)mode, mapId);
                    }
                    else
                    {
                        return item.UseTo((byte)mode);
                    }
                }
            }
        }

        if (action == "inventory.toggle-trainplace")
        {
            if (TryResolveActionItem(payload, out var item))
            {
                var code = item.Record?.CodeName;
                if (!string.IsNullOrEmpty(code))
                    return ToggleConfigArrayItem("UBot.Inventory.ItemsAtTrainplace", code);
            }
        }

        if (action == "inventory.toggle-auto-use")
        {
            if (TryResolveActionItem(payload, out var item))
            {
                var code = item.Record?.CodeName;
                if (!string.IsNullOrEmpty(code))
                    return ToggleConfigArrayItem("UBot.Inventory.AutoUseAccordingToPurpose", code);
            }
        }

        if (action == "inventory.set-auto-sort")
        {
            if (TryGetBoolValue(payload, "value", out var val))
            {
                UBot.Core.RuntimeAccess.Player.Set("UBot.Inventory.AutoSort", val);
                UBot.Core.RuntimeAccess.Player.Save();
                return true;
            }
        }

        return false;
    }

    private static InventoryItem? ResolveInventoryItem(string source, byte slot)
    {
        var player = UBot.Core.RuntimeAccess.Session.Player;
        if (player == null) return null;

        return source.ToLower() switch
        {
            "inventory" => slot >= 13 ? player.Inventory?.GetItemAt(slot) : null,
            "equipment" => slot < 13 ? player.Inventory?.GetItemAt(slot) : null,
            "avatars" => player.Avatars?.GetItemAt(slot),
            "storage" => player.Storage?.GetItemAt(slot),
            "guildstorage" => player.GuildStorage?.GetItemAt(slot),
            "grabpet" => player.AbilityPet?.Inventory?.GetItemAt(slot),
            "jobtransport" => player.JobTransport?.Inventory?.GetItemAt(slot),
            "specialty" => player.Job2SpecialtyBag?.GetItemAt(slot),
            "jobequipment" => player.Job2?.GetItemAt(slot),
            "fellowpet" => player.Fellow?.Inventory?.GetItemAt(slot),
            _ => player.Inventory?.GetItemAt(slot)
        };
    }

    private static bool TryResolveActionItem(Dictionary<string, object?> payload, out InventoryItem item)
    {
        item = null!;
        if (TryGetStringValue(payload, "source", out var source) && TryGetIntValue(payload, "slot", out var slot))
            item = ResolveInventoryItem(source, (byte)slot)!;

        return item != null;
    }

    private static bool ToggleConfigArrayItem(string key, string value)
    {
        var list = UBot.Core.RuntimeAccess.Player.GetArray<string>(key).ToList();
        if (list.Contains(value, StringComparer.OrdinalIgnoreCase))
            list.RemoveAll(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
        else
            list.Add(value);

        UBot.Core.RuntimeAccess.Player.Set(key, list.ToArray());
        UBot.Core.RuntimeAccess.Player.Save();
        return true;
    }
}

internal sealed class UbotPartyActionHandler : UbotServiceBase
{
    internal bool HandlePartyAction(string action, Dictionary<string, object?> payload)
    {
        switch (action)
        {
            case "party.leave":
            case "party.leave-party":
                if (UBot.Core.RuntimeAccess.Session.Party?.IsInParty == true)
                {
                    UBot.Core.RuntimeAccess.Session.Party.Leave();
                    return true;
                }
                return false;

            case "party.banish-member":
                if (!TryGetUIntValue(payload, "memberId", out var memberId))
                    return false;
                if (UBot.Core.RuntimeAccess.Session.Party?.Members == null)
                    return false;

                var member = UBot.Core.RuntimeAccess.Session.Party.Members.FirstOrDefault(item => item.MemberId == memberId);
                if (member == null)
                    return false;

                member.Banish();
                return true;

            case "party.matching.search":
            case "party.matching.refresh":
                return HandlePartyMatchingSearchAction(payload);

            case "party.matching.join":
            case "party.matching.join-party":
                return HandlePartyMatchingJoinAction(payload);

            case "party.matching.form":
            case "party.matching.add":
            case "party.matching.create":
                return HandlePartyMatchingCreateOrChangeAction(payload, false);

            case "party.matching.change":
                return HandlePartyMatchingCreateOrChangeAction(payload, true);

            case "party.matching.delete":
            case "party.matching.delete-entry":
                return HandlePartyMatchingDeleteAction(payload);

            default:
                return false;
        }
    }

    private static bool HandlePartyMatchingSearchAction(Dictionary<string, object?> payload)
    {
        var queryName = TryGetStringValue(payload, "name", out var nameFilter) ? nameFilter.Trim() : string.Empty;
        var queryTitle = TryGetStringValue(payload, "title", out var titleFilter) ? titleFilter.Trim() : string.Empty;
        var requestedPurpose = TryGetIntValue(payload, "purpose", out var purposeFilter)
            ? Math.Clamp(purposeFilter, -1, 3)
            : -1;

        var levelFrom = TryGetIntValue(payload, "levelFrom", out var parsedLevelFrom)
            ? Math.Clamp(parsedLevelFrom, 1, 140)
            : 1;
        var levelTo = TryGetIntValue(payload, "levelTo", out var parsedLevelTo)
            ? Math.Clamp(parsedLevelTo, 1, 140)
            : 140;

        if (levelFrom > levelTo)
            (levelFrom, levelTo) = (levelTo, levelFrom);

        var merged = new List<Dictionary<string, object?>>();
        if (!TryRequestPartyMatchingPage(0, out var pageCount, out var firstPageEntries))
            return false;

        merged.AddRange(firstPageEntries);
        for (byte page = 1; page < pageCount; page++)
        {
            if (!TryRequestPartyMatchingPage(page, out _, out var pageEntries))
                continue;

            merged.AddRange(pageEntries);
        }

        var filtered = merged
            .Where(entry =>
            {
                var leader = entry.TryGetValue("name", out var nameRaw) ? nameRaw?.ToString() ?? string.Empty : string.Empty;
                var title = entry.TryGetValue("title", out var titleRaw) ? titleRaw?.ToString() ?? string.Empty : string.Empty;
                var min = entry.TryGetValue("minLevel", out var minRaw) && TryConvertInt(minRaw, out var parsedMin) ? parsedMin : 1;
                var max = entry.TryGetValue("maxLevel", out var maxRaw) && TryConvertInt(maxRaw, out var parsedMax) ? parsedMax : 140;
                var purpose = entry.TryGetValue("purposeValue", out var purposeRaw) && TryConvertInt(purposeRaw, out var parsedPurpose) ? parsedPurpose : 0;

                if (!string.IsNullOrWhiteSpace(queryName)
                    && !leader.Contains(queryName, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrWhiteSpace(queryTitle)
                    && !title.Contains(queryTitle, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (requestedPurpose >= 0 && purpose != requestedPurpose)
                    return false;

                if (max < levelFrom || min > levelTo)
                    return false;

                return true;
            })
            .ToList();

        for (var i = 0; i < filtered.Count; i++)
            filtered[i]["no"] = i + 1;

        var pluginConfig = LoadPluginJsonConfig(PartyPluginName);
        pluginConfig["matchingResults"] = filtered.Cast<object?>().ToList();
        pluginConfig["matchingSelectedId"] = 0U;
        pluginConfig["matchingQueryName"] = queryName;
        pluginConfig["matchingQueryTitle"] = queryTitle;
        pluginConfig["matchingQueryPurpose"] = requestedPurpose;
        pluginConfig["matchingQueryLevelFrom"] = levelFrom;
        pluginConfig["matchingQueryLevelTo"] = levelTo;
        pluginConfig["matchingLastRefreshAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        SavePluginJsonConfig(PartyPluginName, pluginConfig);
        return true;
    }

    private static bool TryRequestPartyMatchingPage(byte page, out byte pageCount, out List<Dictionary<string, object?>> entries)
    {
        pageCount = 0;
        entries = new List<Dictionary<string, object?>>();
        var success = false;
        byte parsedPageCount = 0;
        var parsedEntries = new List<Dictionary<string, object?>>();

        var packet = new Packet(0x706C);
        packet.WriteByte(page);

        var callback = new AwaitCallback(
            response =>
            {
                if (response.ReadByte() != 1)
                    return AwaitCallbackResult.Fail;

                parsedPageCount = response.ReadByte();
                _ = response.ReadByte();
                var partyCount = response.ReadByte();
                for (var i = 0; i < partyCount; i++)
                    parsedEntries.Add(ParsePartyMatchingEntry(response));

                success = true;
                return AwaitCallbackResult.Success;
            },
            0xB06C
        );

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, callback);
        callback.AwaitResponse(5000);
        pageCount = parsedPageCount;
        entries = parsedEntries;
        return callback.IsCompleted && success;
    }

    private static Dictionary<string, object?> ParsePartyMatchingEntry(Packet packet)
    {
        var id = packet.ReadUInt();
        _ = packet.ReadUInt();

        if (UBot.Core.RuntimeAccess.Session.ClientType >= GameClientType.Chinese && UBot.Core.RuntimeAccess.Session.ClientType != GameClientType.Rigid)
            _ = packet.ReadUInt();

        var leader = packet.ReadString();
        var race = (ObjectCountry)packet.ReadByte();
        var memberCount = packet.ReadByte();
        var settings = PartySettings.FromType(packet.ReadByte());
        var purpose = (PartyPurpose)packet.ReadByte();
        var minLevel = packet.ReadByte();
        var maxLevel = packet.ReadByte();
        var title = packet.ReadConditonalString();

        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["name"] = leader,
            ["title"] = title,
            ["race"] = race.ToString(),
            ["purpose"] = purpose.ToString(),
            ["purposeValue"] = (int)purpose,
            ["memberCount"] = memberCount,
            ["member"] = memberCount,
            ["minLevel"] = (int)minLevel,
            ["maxLevel"] = (int)maxLevel,
            ["range"] = $"{minLevel} ~ {maxLevel}",
            ["expAutoShare"] = settings.ExperienceAutoShare,
            ["itemAutoShare"] = settings.ItemAutoShare,
            ["allowInvitations"] = settings.AllowInvitation
        };
    }

    private static bool HandlePartyMatchingJoinAction(Dictionary<string, object?> payload)
    {
        if (!TryResolveMatchingId(payload, out var matchingId))
            return false;

        var accepted = 0;
        var callback = new AwaitCallback(
            response =>
            {
                var result = response.ReadByte();
                if (result != 1)
                    return AwaitCallbackResult.Fail;

                accepted = response.ReadByte();
                return AwaitCallbackResult.Success;
            },
            0xB06D
        );

        var packet = new Packet(0x706D);
        packet.WriteUInt(matchingId);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, callback);
        callback.AwaitResponse(10000);
        return callback.IsCompleted && accepted == 1;
    }

    private static bool HandlePartyMatchingCreateOrChangeAction(Dictionary<string, object?> payload, bool changeExisting)
    {
        var expAutoShare = TryGetBoolValue(payload, "expAutoShare", out var parsedExpAutoShare)
            ? parsedExpAutoShare
            : UBot.Core.RuntimeAccess.Player.Get("UBot.Party.EXPAutoShare", true);

        var itemAutoShare = TryGetBoolValue(payload, "itemAutoShare", out var parsedItemAutoShare)
            ? parsedItemAutoShare
            : UBot.Core.RuntimeAccess.Player.Get("UBot.Party.ItemAutoShare", true);

        var allowInvitations = TryGetBoolValue(payload, "allowInvitations", out var parsedAllowInvitations)
            ? parsedAllowInvitations
            : UBot.Core.RuntimeAccess.Player.Get("UBot.Party.AllowInvitations", true);

        var purposeValue = TryGetIntValue(payload, "purpose", out var parsedPurpose)
            ? Math.Clamp(parsedPurpose, 0, 3)
            : (int)UBot.Core.RuntimeAccess.Player.Get<byte>("UBot.Party.Matching.Purpose", 0);

        var levelFrom = TryGetIntValue(payload, "levelFrom", out var parsedLevelFrom)
            ? Math.Clamp(parsedLevelFrom, 1, 140)
            : (int)UBot.Core.RuntimeAccess.Player.Get<byte>("UBot.Party.Matching.LevelFrom", 1);

        var levelTo = TryGetIntValue(payload, "levelTo", out var parsedLevelTo)
            ? Math.Clamp(parsedLevelTo, 1, 140)
            : (int)UBot.Core.RuntimeAccess.Player.Get<byte>("UBot.Party.Matching.LevelTo", 140);

        if (levelFrom > levelTo)
            (levelFrom, levelTo) = (levelTo, levelFrom);

        var title = TryGetStringValue(payload, "title", out var parsedTitle)
            ? parsedTitle.Trim()
            : UBot.Core.RuntimeAccess.Player.Get("UBot.Party.Matching.Title", "For opening hunting on the silkroad!");

        if (string.IsNullOrWhiteSpace(title))
            title = "For opening hunting on the silkroad!";

        var autoReform = TryGetBoolValue(payload, "autoReform", out var parsedAutoReform)
            ? parsedAutoReform
            : UBot.Core.RuntimeAccess.Player.Get("UBot.Party.Matching.AutoReform", false);

        var autoAccept = TryGetBoolValue(payload, "autoAccept", out var parsedAutoAccept)
            ? parsedAutoAccept
            : UBot.Core.RuntimeAccess.Player.Get("UBot.Party.Matching.AutoAccept", true);

        UBot.Core.RuntimeAccess.Player.Set("UBot.Party.EXPAutoShare", expAutoShare);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Party.ItemAutoShare", itemAutoShare);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Party.AllowInvitations", allowInvitations);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.Purpose", (byte)purposeValue);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.LevelFrom", (byte)levelFrom);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.LevelTo", (byte)levelTo);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.Title", title);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.AutoReform", autoReform);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.AutoAccept", autoAccept);
        UbotPluginConfigService.ApplyLivePartySettingsFromConfig();
        UbotPluginConfigService.RefreshPartyPluginRuntime();

        uint matchingId = 0;
        if (changeExisting && !TryResolveMatchingId(payload, out matchingId))
            return false;

        var settings = new PartySettings(expAutoShare, itemAutoShare, allowInvitations);
        var opcode = changeExisting ? (ushort)0x706A : (ushort)0x7069;
        var callbackOpcode = changeExisting ? (ushort)0xB06A : (ushort)0xB069;
        var callback = new AwaitCallback(
            response => response.ReadByte() == 1 ? AwaitCallbackResult.Success : AwaitCallbackResult.Fail,
            callbackOpcode
        );

        var packet = new Packet(opcode);
        packet.WriteUInt(changeExisting ? matchingId : 0U);
        packet.WriteUInt(0);
        packet.WriteByte(settings.GetPartyType());
        packet.WriteByte((byte)purposeValue);
        packet.WriteByte((byte)levelFrom);
        packet.WriteByte((byte)levelTo);
        packet.WriteConditonalString(title);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, callback);
        callback.AwaitResponse(5000);
        if (!callback.IsCompleted)
            return false;

        HandlePartyMatchingSearchAction(new Dictionary<string, object?>());
        return true;
    }

    private static bool HandlePartyMatchingDeleteAction(Dictionary<string, object?> payload)
    {
        if (!TryResolveMatchingId(payload, out var matchingId))
            return false;

        var packet = new Packet(0x706B);
        packet.WriteUInt(matchingId);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);

        var pluginConfig = LoadPluginJsonConfig(PartyPluginName);
        if (pluginConfig.TryGetValue("matchingResults", out var rawResults) && rawResults is IList resultList)
        {
            var filtered = new List<object?>();
            foreach (var item in resultList)
            {
                if (item is Dictionary<string, object?> dict
                    && dict.TryGetValue("id", out var idRaw)
                    && TryConvertUIntLoose(idRaw, out var idValue)
                    && idValue == matchingId)
                {
                    continue;
                }

                filtered.Add(item);
            }

            pluginConfig["matchingResults"] = filtered;
            pluginConfig["matchingSelectedId"] = 0U;
            SavePluginJsonConfig(PartyPluginName, pluginConfig);
        }

        return true;
    }

    private static bool TryResolveMatchingId(IDictionary<string, object?> payload, out uint matchingId)
    {
        matchingId = 0;
        if (TryGetUIntValue(payload, "matchingId", out matchingId))
            return matchingId > 0;
        if (TryGetUIntValue(payload, "entryId", out matchingId))
            return matchingId > 0;
        if (TryGetUIntValue(payload, "id", out matchingId))
            return matchingId > 0;
        if (TryGetUIntValue(payload, "selectedId", out matchingId))
            return matchingId > 0;

        var pluginConfig = LoadPluginJsonConfig(PartyPluginName);
        if (pluginConfig.TryGetValue("matchingSelectedId", out var selectedRaw) && TryConvertUIntLoose(selectedRaw, out var selected))
        {
            matchingId = selected;
            return matchingId > 0;
        }

        return false;
    }
}
