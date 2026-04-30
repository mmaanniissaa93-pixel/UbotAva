using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Quests;
using UBot.Core.Objects.Spawn;
using UBot.Protocol;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Protocol.Handlers.Agent.Character;

public class CharacterDataEndResponse : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x34A6;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        SpawnManager.Clear();

        packet = CoreGame.ChunkedPacket;
        packet.Lock();

        if (CoreGame.ClientType >= GameClientType.Thailand)
            packet.ReadUInt(); // serverTimestamp

        var modelId = packet.ReadUInt();

        var character = new Player(modelId);
        character.Scale = packet.ReadByte();
        character.Level = packet.ReadByte();
        character.MaxLevel = packet.ReadByte();
        character.Experience = packet.ReadLong();
        character.SkillExperience = packet.ReadUInt();
        character.Gold = packet.ReadULong();
        character.SkillPoints = packet.ReadUInt();
        character.StatPoints = packet.ReadUShort();
        character.BerzerkPoints = packet.ReadByte();
        character.ExperienceChunk = packet.ReadUInt();
        character.Health = packet.ReadInt();
        character.Mana = packet.ReadInt();
        character.AutoInverstExperience = (AutoInverstType)packet.ReadByte();

        if (CoreGame.ClientType == GameClientType.Chinese_Old)
            character.DailyPK = (byte)packet.ReadUShort();
        else
            character.DailyPK = packet.ReadByte();

        character.TotalPK = packet.ReadUShort();
        character.PKPenaltyPoint = packet.ReadUInt();

        if (CoreGame.ClientType >= GameClientType.Thailand)
            character.BerzerkLevel = packet.ReadByte();

        if (CoreGame.ClientType > GameClientType.Thailand)
            /*character.PvpFlag = (PvpFlag)*/packet.ReadByte();

        if (CoreGame.ClientType >= GameClientType.Chinese)
        {
            if (CoreGame.ClientType != GameClientType.Chinese)
                packet.ReadByte();

            packet.ReadUInt(); //You can use VIP service until this time
            packet.ReadByte();

            if (
                CoreGame.ClientType == GameClientType.Turkey
                || CoreGame.ClientType == GameClientType.VTC_Game
                || CoreGame.ClientType == GameClientType.RuSro
                || CoreGame.ClientType == GameClientType.Taiwan
            )
                packet.ReadUInt();

            if (CoreGame.ClientType == GameClientType.Rigid)
                packet.ReadBytes(12);

            if (CoreGame.ClientType == GameClientType.VTC_Game)
                packet.ReadByte(); // ??

            if (CoreGame.ClientType == GameClientType.Taiwan)
                packet.ReadBytes(5);

            var serverCap = packet.ReadByte();
            Log.Notify($"The game server cap is {serverCap}!");
            if (serverCap == 0)
            {
                Log.Warn(
                    $"Server cap parsed as 0 for client type [{CoreGame.ClientType}]. This usually indicates a client type/protocol mismatch."
                );
                CoreGame.ChunkedPacket = null;
                return;
            }

            if (
                CoreGame.ClientType != GameClientType.Korean
                && CoreGame.ClientType != GameClientType.Chinese
                && CoreGame.ClientType != GameClientType.Japanese
            )
                packet.ReadUShort();
        }

        character.Inventory = new CharacterInventory();
        character.Inventory.Deserialize(packet);

        if (CoreGame.ClientType >= GameClientType.Thailand)
            character.Avatars = packet.ReadInventoryItemCollection();
        else
            character.Avatars = new InventoryItemCollection(5);

        // JOB2
        if (CoreGame.ClientType > GameClientType.Vietnam)
        {
            character.Job2SpecialtyBag = packet.ReadInventoryItemCollection();

            character.Job2 = packet.ReadInventoryItemCollection();
        }

        character.Skills = packet.ReadSkills();
        character.QuestLog = packet.ReadQuestLog();

        packet.ReadByte(); // Unknown

        if (CoreGame.ClientType > GameClientType.Thailand)
        {
            var collectionBookStartedThemeCount = packet.ReadUInt();
            for (var i = 0; i < collectionBookStartedThemeCount; i++)
            {
                packet.ReadUInt(); //index
                packet.ReadUInt(); //Starttime
                packet.ReadUInt(); //pages
            }
        }

        character.ParseBionicDetails(packet);

        character.Name = packet.ReadString();
        character.JobInformation = packet.ReadJobInfo();
        character.State.PvpState = (PvpState)packet.ReadByte();
        character.OnTransport = packet.ReadBool(); //On transport?
        character.InCombat = packet.ReadBool();

        if (CoreGame.ClientType >= GameClientType.Chinese)
            packet.ReadByte();

        if (character.OnTransport)
            character.TransportUniqueId = packet.ReadUInt();

        if (CoreGame.ClientType >= GameClientType.Chinese)
            packet.ReadUInt(); //unkUint2 i think it is using for balloon event or buff for events

        if (CoreGame.ClientType > GameClientType.Vietnam)
            packet.ReadByte();

        packet.ReadByte(); //PVP dress for the CTF event //0 = Red Side, 1 = Blue Side, 0xFF = None

        if (
            CoreGame.ClientType > GameClientType.Chinese
            && CoreGame.ClientType != GameClientType.Global
            && CoreGame.ClientType != GameClientType.Rigid
            && CoreGame.ClientType != GameClientType.RuSro
            && CoreGame.ClientType != GameClientType.Korean
            && CoreGame.ClientType != GameClientType.VTC_Game
            && CoreGame.ClientType != GameClientType.Japanese
        )
        {
            packet.ReadByte(); // 0xFF
            packet.ReadUShort(); // 0xFF
            packet.ReadUShort(); // 0xFF
        }

        //GuideFlag
        if (CoreGame.ClientType >= GameClientType.Thailand)
            packet.ReadULong();
        else
            packet.ReadUInt();

        if (
            CoreGame.ClientType == GameClientType.Chinese_Old
            || CoreGame.ClientType == GameClientType.Chinese
            || CoreGame.ClientType == GameClientType.Global
            || CoreGame.ClientType == GameClientType.RuSro
            || CoreGame.ClientType == GameClientType.Korean
            || CoreGame.ClientType == GameClientType.VTC_Game
            || CoreGame.ClientType == GameClientType.Japanese
        )
            packet.ReadByte();

        if (CoreGame.ClientType == GameClientType.Chinese)
            packet.ReadByte();

        character.JID = packet.ReadUInt();
        character.IsGameMaster = packet.ReadBool();

        // Load Notification sound settings
        character.NotificationSounds = ProtocolRuntime.LegacyRuntime.CreateNotificationSounds();
        ProtocolRuntime.LegacyRuntime.LoadNotificationSounds(character.NotificationSounds);

        //Set instance..
        CoreGame.Player = character;
        CoreGame.ChunkedPacket = null;

        EventManager.FireEvent("OnLoadCharacter");

        ClientManager.SetTitle($"{character.Name} - UBot");

        if (!CoreGame.Clientless)
            return;

        ProtocolRuntime.Dispatch(new Packet(0x3012), PacketDestination.Server);
        CoreGame.Ready = true;
    }
}






