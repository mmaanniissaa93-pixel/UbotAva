using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UBot.Core;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;

namespace UBot.Party.Bundle.Commands;

internal class CommandsBundle
{
    public CommandsConfig Config { get; set; }

    private readonly Dictionary<string, Action<CommandContext>> _commands;
    private static readonly Random _random = new(Environment.TickCount);

    private bool _followActivated;
    private string _followTargetName = string.Empty;
    private float _followDistance;
    private int _lastFollowTick;

    internal CommandsBundle()
    {
        _commands = new(StringComparer.InvariantCultureIgnoreCase)
        {
            ["start"] = StartBot,
            ["stop"] = StopBot,
            ["trace"] = Trace,
            ["notrace"] = StopTrace,
            ["town"] = ReturnTown,
            ["return"] = ReturnTown,
            ["radius"] = SetRadius,
            ["setradius"] = SetRadius,
            ["setpos"] = SetPosition,
            ["area"] = SetAreaByCoordinates,
            ["setarea"] = SetAreaByName,
            ["moveon"] = MoveOn,
            ["follow"] = Follow,
            ["nofollow"] = StopFollowCommand,
            ["sitdown"] = SitToggle,
            ["sit"] = SitToggle,
            ["jump"] = Jump,
            ["zerk"] = Zerk,
            ["getout"] = LeaveParty,
            ["mount"] = Mount,
            ["dismount"] = Dismount,
            ["equip"] = Equip,
            ["unequip"] = Unequip,
            ["use"] = UseItem,
            ["cape"] = Cape,
            ["profile"] = Profile,
            ["chat"] = Chat,
            ["getpos"] = GetPosition,
            ["dc"] = Disconnect,
            ["tp"] = Teleport,
            ["recall"] = Recall,
            ["inject"] = Inject,
        };

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTick", OnTick);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnAgentServerDisconnected", StopFollow);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnLoadCharacter", StopFollow);
    }

    public void Handle(string senderName, SpawnedPlayer sender, ChatType chatType, string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(senderName) || string.IsNullOrWhiteSpace(message))
                return;

            if (!IsSenderAllowed(senderName))
                return;

            var args = message.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 0)
                return;

            var commandToken = args[0].Trim();
            // xControl behavior: execute only explicit uppercased commands to avoid triggering on normal chat.
            if (!commandToken.Equals(commandToken.ToUpperInvariant(), StringComparison.InvariantCulture))
                return;

            var command = commandToken.ToLowerInvariant();
            var parameters = args.Length > 1 ? args[1].Trim() : string.Empty;
            if (!_commands.TryGetValue(command, out var action))
                return;

            var context = new CommandContext(senderName, sender, chatType, command, parameters);
            action(context);
        }
        catch (Exception e)
        {
            Log.Fatal(e);
        }
    }

    public void Refresh()
    {
        Config = new CommandsConfig
        {
            PlayerList = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Party.Commands.PlayersList"),
            ListenFromList = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.Commands.ListenOnlyList"),
            ListenOnlyMaster = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.Commands.ListenFromMaster"),
        };
    }

    private bool IsSenderAllowed(string senderName)
    {
        if (Config == null)
            return false;

        var listAllowed = Config.ListenFromList
            && Config.PlayerList != null
            && Config.PlayerList.Any(name =>
                name.Equals(senderName, StringComparison.InvariantCultureIgnoreCase)
            );

        var masterAllowed = Config.ListenOnlyMaster
            && UBot.Core.RuntimeAccess.Session.Party?.Leader != null
            && UBot.Core.RuntimeAccess.Session.Party.Leader.Name.Equals(senderName, StringComparison.InvariantCultureIgnoreCase);

        return listAllowed || masterAllowed;
    }

    private static void StartBot(CommandContext context)
    {
        UBot.Core.RuntimeAccess.Core.Bot.Start();
    }

    private static void StopBot(CommandContext context)
    {
        UBot.Core.RuntimeAccess.Core.Bot.Stop();
    }

    private static void Trace(CommandContext context)
    {
        var targetName = GetFirstTokenOrDefault(context.Arguments, context.SenderName);
        if (string.IsNullOrWhiteSpace(targetName))
            return;

        if (!TryFindSpawnedPlayer(targetName, out var player))
        {
            Log.Warn($"[Party.Commands] Could not trace [{targetName}] because the player is not visible.");
            return;
        }

        var packet = new Packet(0x7074);
        packet.WriteByte((byte)ActionCommandType.Execute);
        packet.WriteByte((byte)ActionType.Trace);
        packet.WriteByte((byte)ActionTarget.Entity);
        packet.WriteUInt(player.UniqueId);

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private static void StopTrace(CommandContext context)
    {
        SkillManager.CancelAction();
    }

    private static void ReturnTown(CommandContext context)
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.State.LifeState == LifeState.Dead)
        {
            var packet = new Packet(0x3053);
            packet.WriteByte(UBot.Core.RuntimeAccess.Session.Player.Level < 10 ? 2 : 1);
            UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
            return;
        }

        UBot.Core.RuntimeAccess.Session.Player.UseReturnScroll();
    }

    private static void SetRadius(CommandContext context)
    {
        var radius = 35;
        if (!string.IsNullOrWhiteSpace(context.Arguments))
        {
            var argument = context.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            if (!TryParseFloat(argument, out var parsed))
                return;

            radius = Math.Abs((int)parsed);
        }

        radius = Math.Clamp(radius, 5, 100);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Radius", radius);
        UBot.Core.RuntimeAccess.Events.FireEvent("OnSetTrainingArea");
    }

    private static void SetPosition(CommandContext context)
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            SetTrainingArea(UBot.Core.RuntimeAccess.Session.Player.Position, UBot.Core.RuntimeAccess.Player.Get("UBot.Area.Radius", 50));
            return;
        }

        var args = context.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length < 2)
            return;

        if (!TryParseFloat(args[0], out var x) || !TryParseFloat(args[1], out var y))
            return;

        var region = UBot.Core.RuntimeAccess.Session.Player.Position.Region;
        if (args.Length >= 3 && ushort.TryParse(args[2], out var parsedRegion))
            region = parsedRegion;

        var z = UBot.Core.RuntimeAccess.Session.Player.Position.ZOffset;
        if (args.Length >= 4 && TryParseFloat(args[3], out var parsedZ))
            z = parsedZ;

        var position = new Position(x, y, region)
        {
            ZOffset = z,
        };

        SetTrainingArea(position, UBot.Core.RuntimeAccess.Player.Get("UBot.Area.Radius", 50));
    }

    private static void SetAreaByCoordinates(CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments) || UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var args = context.Arguments.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (args.Length != 3)
            return;

        if (!TryParseFloat(args[0], out var x) || !TryParseFloat(args[1], out var y) || !TryParseFloat(args[2], out var radius))
            return;

        var position = new Position(x, y, UBot.Core.RuntimeAccess.Session.Player.Position.Region);
        SetTrainingArea(position, Math.Clamp((int)Math.Abs(radius), 5, 100));
    }

    private static void SetAreaByName(CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
            return;

        var areas = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Training.Areas");
        foreach (var areaString in areas)
        {
            var split = areaString.Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 6)
                continue;

            if (!Area.TryParse(split, out var area))
                continue;

            if (!area.Name.Equals(context.Arguments, StringComparison.InvariantCultureIgnoreCase))
                continue;

            UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Region", area.Position.Region);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Area.X", area.Position.XOffset);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Y", area.Position.YOffset);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Z", area.Position.ZOffset);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Radius", area.Radius);
            UBot.Core.RuntimeAccess.Events.FireEvent("OnSetTrainingArea");
            return;
        }
    }

    private static void MoveOn(CommandContext context)
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var radius = 10f;
        if (!string.IsNullOrWhiteSpace(context.Arguments))
        {
            var args = context.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length > 0 && TryParseFloat(args[0], out var parsed))
                radius = Math.Abs(parsed);
        }

        var source = UBot.Core.RuntimeAccess.Session.Player.Position;
        var x = source.X + ((float)_random.NextDouble() * 2f - 1f) * radius;
        var y = source.Y + ((float)_random.NextDouble() * 2f - 1f) * radius;
        var destination = new Position(x, y, source.Region)
        {
            ZOffset = source.ZOffset,
        };

        UBot.Core.RuntimeAccess.Session.Player.MoveTo(destination, false);
    }

    private void Follow(CommandContext context)
    {
        if (!UBot.Core.RuntimeAccess.Session.Party.IsInParty)
            return;

        var args = context.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var targetName = args.Length > 0 ? args[0] : context.SenderName;
        var distance = 10f;

        if (args.Length > 1 && TryParseFloat(args[1], out var parsedDistance))
            distance = Math.Max(0f, parsedDistance);

        if (!IsInParty(targetName))
            return;

        _followActivated = true;
        _followTargetName = targetName;
        _followDistance = distance;
        _lastFollowTick = 0;
    }

    private void StopFollowCommand(CommandContext context)
    {
        StopFollow();
    }

    private static void SitToggle(CommandContext context)
    {
        var packet = new Packet(0x704F);
        packet.WriteByte(4);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private static void Jump(CommandContext context)
    {
        var packet = new Packet(0x3091);
        packet.WriteByte(0x0C);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private static void Zerk(CommandContext context)
    {
        UBot.Core.RuntimeAccess.Session.Player?.EnterBerzerkMode();
    }

    private static void LeaveParty(CommandContext context)
    {
        if (UBot.Core.RuntimeAccess.Session.Party?.IsInParty == true)
            UBot.Core.RuntimeAccess.Session.Party.Leave();
    }

    private static void Mount(CommandContext context)
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var petType = GetFirstTokenOrDefault(context.Arguments, "horse");
        if (!petType.Equals("horse", StringComparison.InvariantCultureIgnoreCase)
            && !petType.Equals("vehicle", StringComparison.InvariantCultureIgnoreCase))
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle)
        {
            UBot.Core.RuntimeAccess.Session.Player.Vehicle.Mount();
            return;
        }

        UBot.Core.RuntimeAccess.Session.Player.SummonVehicle();
    }

    private static void Dismount(CommandContext context)
    {
        if (UBot.Core.RuntimeAccess.Session.Player?.HasActiveVehicle == true)
            UBot.Core.RuntimeAccess.Session.Player.Vehicle.Dismount();
    }

    private static void Equip(CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments) || UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var item = FindItemByName(context.Arguments, equippedOnly: false);
        if (item == null || !item.CanBeEquipped())
            return;

        var slot = ResolveEquipSlot(item);
        if (slot == null)
            return;

        item.Equip(slot.Value);
    }

    private static void Unequip(CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments) || UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var item = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItems(i =>
            i.Slot < CharacterInventory.NORMAL_PART_MIN_SLOT && IsMatchingItem(i, context.Arguments)
        ).FirstOrDefault();

        if (item == null)
            return;

        var freeSlot = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetFreeSlot();
        if (freeSlot < CharacterInventory.NORMAL_PART_MIN_SLOT)
            return;

        UBot.Core.RuntimeAccess.Session.Player.Inventory.MoveItem(item.Slot, freeSlot);
    }

    private static void UseItem(CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments) || UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var item = FindItemByName(context.Arguments, equippedOnly: false);
        item?.Use();
    }

    private static void Cape(CommandContext context)
    {
        var requestedCape = GetFirstTokenOrDefault(context.Arguments, "yellow").ToLowerInvariant();
        byte mode = requestedCape switch
        {
            "off" => 0x00,
            "red" => 0x01,
            "gray" => 0x02,
            "blue" => 0x03,
            "white" => 0x04,
            "yellow" => 0x05,
            _ => 0x05,
        };

        var packet = new Packet(0x7516);
        packet.WriteByte(mode);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private static void Profile(CommandContext context)
    {
        var requestedProfile = string.IsNullOrWhiteSpace(context.Arguments) ? "Default" : context.Arguments.Trim();
        var profile = ProfileManager.Profiles.FirstOrDefault(p =>
            p.Equals(requestedProfile, StringComparison.InvariantCultureIgnoreCase)
        );

        if (string.IsNullOrWhiteSpace(profile))
            return;

        if (ProfileManager.SetSelectedProfile(profile))
            UBot.Core.RuntimeAccess.Events.FireEvent("OnProfileChanged");
    }

    private static void Chat(CommandContext context)
    {
        var args = context.Arguments.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (args.Length != 2)
            return;

        var chatKind = args[0].ToLowerInvariant();
        var payload = args[1];

        switch (chatKind)
        {
            case "all":
                SendChatPacket(ChatType.All, payload);
                break;
            case "party":
                SendChatPacket(ChatType.Party, payload);
                break;
            case "guild":
                SendChatPacket(ChatType.Guild, payload);
                break;
            case "union":
                SendChatPacket(ChatType.Union, payload);
                break;
            case "stall":
                SendChatPacket(ChatType.Stall, payload);
                break;
            case "global":
                SendGlobalChatPacket(payload);
                break;
            case "private":
            case "note":
                var privateArgs = payload.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (privateArgs.Length != 2)
                    return;

                SendChatPacket(ChatType.Private, privateArgs[1], privateArgs[0]);
                break;
        }
    }

    private static void GetPosition(CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.SenderName) || UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var pos = UBot.Core.RuntimeAccess.Session.Player.Position;
        var message =
            $"My position is (X:{pos.X:0.0},Y:{pos.Y:0.0},Z:{pos.ZOffset:0.0},Region:{pos.Region})";

        SendChatPacket(ChatType.Private, message, context.SenderName);
    }

    private static void Disconnect(CommandContext context)
    {
        UBot.Core.RuntimeAccess.Core.Proxy?.Server?.Disconnect();
    }

    private static void Teleport(CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
            return;

        var args = context.Arguments.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (args.Length < 2)
            args = context.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (args.Length < 2)
            return;

        var sourceName = args[0];
        var destinationName = args[1];

        var sourceTeleport = FindTeleport(sourceName, requireNearbyNpc: true, out var sourceNpc);
        if (sourceTeleport == null || sourceNpc == null)
            return;

        var link = sourceTeleport.GetLinks().FirstOrDefault(l => IsMatchingTeleport(l.Target, destinationName));
        if (link == null && int.TryParse(destinationName, out var destinationId))
            link = sourceTeleport.GetLinks().FirstOrDefault(l => l.TargetTeleport == destinationId);

        if (link?.Target == null)
            return;

        sourceNpc.TrySelect();

        var packet = new Packet(0x705A);
        packet.WriteUInt(sourceNpc.UniqueId);
        packet.WriteByte(0x02);
        packet.WriteUInt(link.Target.ID);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private static void Recall(CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
            return;

        if (!TryFindNpc(context.Arguments, out var npc))
            return;

        var packet = new Packet(0x7059);
        packet.WriteUInt(npc.UniqueId);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private static void Inject(CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
            return;

        var args = context.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0 || !TryParseUShortHex(args[0], out var opcode))
            return;

        var encrypted = false;
        var dataIndex = 1;
        if (args.Length > 1 && bool.TryParse(args[1], out var encryptedParsed))
        {
            encrypted = encryptedParsed;
            dataIndex = 2;
        }

        var packet = new Packet(opcode, encrypted);

        for (var i = dataIndex; i < args.Length; i++)
        {
            if (!TryParseByteHex(args[i], out var value))
                return;

            packet.WriteByte(value);
        }

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private void OnTick()
    {
        if (!_followActivated || UBot.Core.RuntimeAccess.Session.Player == null || !UBot.Core.RuntimeAccess.Session.Ready)
            return;

        if (UBot.Core.RuntimeAccess.Core.TickCount - _lastFollowTick < 800)
            return;

        _lastFollowTick = UBot.Core.RuntimeAccess.Core.TickCount;

        if (!TryGetPartyMemberPosition(_followTargetName, out var destination))
            return;

        var source = UBot.Core.RuntimeAccess.Session.Player.Position;
        if (source.Region != destination.Region)
            return;

        var distance = source.DistanceTo(destination);
        if (distance <= _followDistance + 0.5f)
            return;

        var movementDistance = distance - _followDistance;
        if (movementDistance <= 0.1f)
            return;

        var unitX = (destination.X - source.X) / distance;
        var unitY = (destination.Y - source.Y) / distance;

        var targetX = source.X + (float)(movementDistance * unitX);
        var targetY = source.Y + (float)(movementDistance * unitY);
        var followPosition = new Position(targetX, targetY, destination.Region)
        {
            ZOffset = destination.ZOffset,
        };

        MoveToWithoutAwait(followPosition);
    }

    private void StopFollow()
    {
        _followActivated = false;
        _followTargetName = string.Empty;
        _followDistance = 0f;
        _lastFollowTick = 0;
    }

    private static bool TryGetPartyMemberPosition(string name, out Position position)
    {
        position = default;
        if (string.IsNullOrWhiteSpace(name) || UBot.Core.RuntimeAccess.Session.Player == null)
            return false;

        if (TryFindSpawnedPlayer(name, out var player))
        {
            position = player.Position;
            return true;
        }

        var member = UBot.Core.RuntimeAccess.Session.Party?.Members?.FirstOrDefault(m =>
            m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
        );
        if (member == null)
            return false;

        position = member.Position;
        return true;
    }

    private static bool IsInParty(string name)
    {
        if (!UBot.Core.RuntimeAccess.Session.Party.IsInParty || string.IsNullOrWhiteSpace(name))
            return false;

        if (UBot.Core.RuntimeAccess.Session.Player.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return UBot.Core.RuntimeAccess.Session.Party.Members.Any(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }

    private static InventoryItem FindItemByName(string name, bool equippedOnly)
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null || string.IsNullOrWhiteSpace(name))
            return null;

        return UBot.Core.RuntimeAccess.Session.Player.Inventory
            .GetItems(i =>
                (equippedOnly
                    ? i.Slot < CharacterInventory.NORMAL_PART_MIN_SLOT
                    : i.Slot >= CharacterInventory.NORMAL_PART_MIN_SLOT)
                && IsMatchingItem(i, name)
            )
            .FirstOrDefault();
    }

    private static bool IsMatchingItem(InventoryItem item, string query)
    {
        if (item == null || string.IsNullOrWhiteSpace(query))
            return false;

        var normalizedQuery = query.Trim();
        return item.Record.CodeName.Equals(normalizedQuery, StringComparison.InvariantCultureIgnoreCase)
            || item.Record.GetRealName().Contains(normalizedQuery, StringComparison.InvariantCultureIgnoreCase);
    }

    private static byte? ResolveEquipSlot(InventoryItem item)
    {
        var record = item.Record;
        if (!record.IsEquip)
            return null;

        if (record.IsArmor)
        {
            return record.TypeID4 switch
            {
                1 => 0,
                2 => 2,
                3 => 1,
                4 => 4,
                5 => 3,
                6 => 5,
                _ => null,
            };
        }

        if (record.IsShield)
            return 7;

        if (record.IsAccessory)
        {
            return record.TypeID4 switch
            {
                1 => 9,
                2 => 10,
                3 => UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItemAt(11) == null ? (byte)11 : (byte)12,
                _ => null,
            };
        }

        if (record.IsWeapon)
            return 6;

        if (record.IsJobOutfit || record.TypeID3 == 7)
            return 8;

        return null;
    }

    private static bool TryFindSpawnedPlayer(string name, out SpawnedPlayer player)
    {
        return SpawnManager.TryGetEntity(
            p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase),
            out player
        );
    }

    private static bool TryFindNpc(string name, out SpawnedNpc npc)
    {
        return SpawnManager.TryGetEntity(
            s => s.Record != null
                && (
                    s.Record.CodeName.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                    || s.Record.GetRealName().Equals(name, StringComparison.InvariantCultureIgnoreCase)
                    || s.Record.GetRealName().Contains(name, StringComparison.InvariantCultureIgnoreCase)
                ),
            out npc
        );
    }

    private static RefTeleport FindTeleport(string query, bool requireNearbyNpc, out SpawnedNpc npc)
    {
        npc = null;

        foreach (var teleport in UBot.Core.RuntimeAccess.Session.ReferenceManager.TeleportData.Where(t => IsMatchingTeleport(t, query)))
        {
            if (teleport.Character == null)
                continue;

            if (
                SpawnManager.TryGetEntity(
                    s => s.Record.CodeName.Equals(teleport.Character.CodeName, StringComparison.InvariantCultureIgnoreCase),
                    out SpawnedNpc foundNpc
                )
            )
            {
                npc = foundNpc;
                return teleport;
            }
        }

        if (!requireNearbyNpc)
            return UBot.Core.RuntimeAccess.Session.ReferenceManager.TeleportData.FirstOrDefault(t => IsMatchingTeleport(t, query));

        return null;
    }

    private static bool IsMatchingTeleport(RefTeleport teleport, string query)
    {
        if (teleport == null || string.IsNullOrWhiteSpace(query))
            return false;

        var q = query.Trim();
        return teleport.ID.ToString() == q
            || teleport.CodeName.Equals(q, StringComparison.InvariantCultureIgnoreCase)
            || teleport.CodeName.Contains(q, StringComparison.InvariantCultureIgnoreCase)
            || teleport.ZoneName.Equals(q, StringComparison.InvariantCultureIgnoreCase)
            || teleport.ZoneName.Contains(q, StringComparison.InvariantCultureIgnoreCase);
    }

    private static void SetTrainingArea(Position position, int radius)
    {
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Region", position.Region);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.X", position.XOffset);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Y", position.YOffset);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Z", position.ZOffset);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Radius", Math.Clamp(radius, 5, 100));
        UBot.Core.RuntimeAccess.Events.FireEvent("OnSetTrainingArea");
    }

    private static string GetFirstTokenOrDefault(string text, string fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        var token = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(token) ? fallback : token;
    }

    private static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
            || float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
    }

    private static bool TryParseUShortHex(string value, out ushort result)
    {
        value = value.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase) ? value[2..] : value;
        return ushort.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseByteHex(string value, out byte result)
    {
        value = value.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase) ? value[2..] : value;
        return byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    private static void SendChatPacket(ChatType type, string message, string receiver = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var chatPacket = new Packet(0x7025);
        chatPacket.WriteByte(type);
        chatPacket.WriteByte(1);

        if (UBot.Core.RuntimeAccess.Session.ClientType > GameClientType.Vietnam)
            chatPacket.WriteByte(0);

        if (UBot.Core.RuntimeAccess.Session.ClientType >= GameClientType.Chinese_Old)
            chatPacket.WriteByte(0);

        if (type == ChatType.Private)
        {
            if (string.IsNullOrWhiteSpace(receiver))
                return;

            chatPacket.WriteString(receiver);
        }

        chatPacket.WriteConditonalString(message);
        UBot.Core.RuntimeAccess.Packets.SendPacket(chatPacket, PacketDestination.Server);
    }

    private static void SendGlobalChatPacket(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var globalItem = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItem(new TypeIdFilter(3, 3, 3, 5));
        if (globalItem == null)
            return;

        var packet = new Packet(0x704C);
        packet.WriteByte(globalItem.Slot);

        if (UBot.Core.RuntimeAccess.Session.ClientType > GameClientType.Vietnam)
        {
            packet.WriteInt(globalItem.Record.Tid);
            packet.WriteByte(0);
        }
        else
        {
            packet.WriteUShort(globalItem.Record.Tid);
        }

        packet.WriteConditonalString(message);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private static void MoveToWithoutAwait(Position destination)
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var packet = new Packet(0x7021);
        packet.WriteByte(1);

        if (!UBot.Core.RuntimeAccess.Session.Player.IsInDungeon)
        {
            packet.WriteUShort(destination.Region);
            packet.WriteShort(destination.XOffset);
            packet.WriteShort(destination.ZOffset);
            packet.WriteShort(destination.YOffset);
        }
        else
        {
            packet.WriteUShort(UBot.Core.RuntimeAccess.Session.Player.Position.Region);
            packet.WriteInt(destination.XOffset);
            packet.WriteInt(destination.ZOffset);
            packet.WriteInt(destination.YOffset);
        }

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    private readonly struct CommandContext
    {
        public CommandContext(string senderName, SpawnedPlayer sender, ChatType chatType, string command, string arguments)
        {
            SenderName = senderName;
            Sender = sender;
            ChatType = chatType;
            Command = command;
            Arguments = arguments;
        }

        public string SenderName { get; }
        public SpawnedPlayer Sender { get; }
        public ChatType ChatType { get; }
        public string Command { get; }
        public string Arguments { get; }
    }
}
