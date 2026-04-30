using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Cos;

public class CosUpdateResponse : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x30C9;

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
        var uniqueId = packet.ReadUInt();
        var type = packet.ReadByte();

        if (CoreGame.Player.Growth?.UniqueId == uniqueId)
        {
            switch (type)
            {
                case 1:
                    EventManager.FireEvent("OnTerminateCos", CoreGame.Player.Growth);
                    CoreGame.Player.Growth = null;
                    break;

                case 2: // update inventory

                    break;

                case 3:
                    var experience = packet.ReadLong();
                    var source = packet.ReadUInt();
                    if (source == CoreGame.Player.Growth.UniqueId)
                        return;

                    CoreGame.Player.Growth.Experience += experience;

                    var iLevel = CoreGame.Player.Growth.Level;
                    while (CoreGame.Player.Growth.Experience > CoreGame.ReferenceManager.GetRefLevel(iLevel).Exp_C)
                    {
                        CoreGame.Player.Growth.Experience -= CoreGame.ReferenceManager.GetRefLevel(iLevel).Exp_C;
                        iLevel++;
                    }

                    if (CoreGame.Player.Growth.Level < iLevel)
                    {
                        CoreGame.Player.Growth.Level = iLevel;
                        EventManager.FireEvent("OnGrowthLevelUp");
                        Log.Notify(
                            $"Congratulations, your pet [{CoreGame.Player.Growth.Name}] level has increased to [{CoreGame.Player.Growth.Level}]"
                        );
                    }

                    EventManager.FireEvent("OnGrowthExperienceUpdate");
                    break;

                case 4:
                    CoreGame.Player.Growth.CurrentHungerPoints = packet.ReadUShort();
                    EventManager.FireEvent("OnGrowthHungerUpdate");
                    break;

                case 5:
                    CoreGame.Player.Growth.Name = packet.ReadString();
                    EventManager.FireEvent("OnGrowthNameChange");
                    break;

                case 7:

                    CoreGame.Player.Growth.Id = packet.ReadUInt();
                    var record = CoreGame.Player.Growth.Record;
                    if (record != null)
                        CoreGame.Player.Growth.Health = CoreGame.Player.Growth.MaxHealth = record.MaxHealth;

                    break;

                default:

                    Log.Debug("Pet update: " + type.ToString("X"));
                    break;
            }
        }
        else if (CoreGame.Player.Fellow?.UniqueId == uniqueId)
        {
            switch (type)
            {
                case 1:
                    EventManager.FireEvent("OnTerminateCos", CoreGame.Player.Fellow);
                    CoreGame.Player.Fellow = null;
                    EventManager.FireEvent("OnTerminateFellow");
                    break;

                case 2: // update inventory

                    break;

                case 3:
                    var experience = packet.ReadLong();
                    var source = packet.ReadUInt();
                    if (source == CoreGame.Player.Fellow.UniqueId)
                        return;

                    CoreGame.Player.Fellow.Experience += experience;

                    var iLevel = CoreGame.Player.Fellow.Level;
                    while (CoreGame.Player.Fellow.Experience > CoreGame.ReferenceManager.GetRefLevel(iLevel).Exp_C_Pet2)
                    {
                        CoreGame.Player.Fellow.Experience -= CoreGame.ReferenceManager.GetRefLevel(iLevel).Exp_C_Pet2;
                        iLevel++;
                    }

                    if (CoreGame.Player.Fellow.Level < iLevel)
                    {
                        CoreGame.Player.Fellow.Level = iLevel;
                        CoreGame.Player.Fellow.MaxHealth = CoreGame.Player.Fellow.Health;
                        EventManager.FireEvent("OnFellowLevelUp");
                        Log.Notify(
                            $"Congratulations, your fellow pet [{CoreGame.Player.Fellow.Name}] level has increased to [{CoreGame.Player.Fellow.Level}]"
                        );
                    }

                    EventManager.FireEvent("OnFellowExperienceUpdate");
                    break;

                case 4:
                    CoreGame.Player.Fellow.Satiety = packet.ReadUShort();
                    EventManager.FireEvent("OnFellowSatietyUpdate");
                    break;

                case 5:
                    CoreGame.Player.Fellow.Name = packet.ReadString();
                    EventManager.FireEvent("OnFellowNameChange");
                    break;

                case 7:

                    CoreGame.Player.Fellow.Id = packet.ReadUInt();
                    var record = CoreGame.Player.Fellow.Record;
                    if (record != null)
                        CoreGame.Player.Fellow.Health = CoreGame.Player.Fellow.MaxHealth = record.MaxHealth;

                    break;

                case 8:
                    packet.ReadULong(); //gained pet exp
                    packet.ReadULong(); //gained skill exp
                    packet.ReadUInt(); //total stored SP
                    packet.ReadUInt(); //mob id
                    break;

                default:

                    Log.Debug("Pet update: " + type.ToString("X"));
                    break;
            }
        }
        else if (CoreGame.Player.AbilityPet?.UniqueId == uniqueId)
        {
            switch (type)
            {
                case 1:
                    EventManager.FireEvent("OnTerminateCos", CoreGame.Player.AbilityPet);
                    CoreGame.Player.AbilityPet = null;
                    EventManager.FireEvent("OnTerminateAbilityPet");
                    break;

                case 2:
                    CoreGame.Player.AbilityPet.Inventory.Deserialize(packet);

                    EventManager.FireEvent("OnUpdateAbilityPetInventorySize");
                    break;

                case 5:
                    CoreGame.Player.AbilityPet.Name = packet.ReadString();
                    EventManager.FireEvent("OnAbilityPetNameChange");
                    break;
            }
        }
        else if (CoreGame.Player.Transport?.UniqueId == uniqueId)
        {
            EventManager.FireEvent("OnTerminateCos", CoreGame.Player.Transport);
            CoreGame.Player.Transport = null;
            EventManager.FireEvent("OnTerminateVehicle");
        }
        else if (CoreGame.Player.JobTransport?.UniqueId == uniqueId)
        {
            switch (type)
            {
                case 1:
                    EventManager.FireEvent("OnTerminateCos", CoreGame.Player.JobTransport);
                    CoreGame.Player.JobTransport = null;
                    EventManager.FireEvent("OnTerminateJobTransport");
                    break;

                case 2:
                    CoreGame.Player.JobTransport.Inventory.Deserialize(packet);

                    EventManager.FireEvent("OnUpdateJobTransportInventory");
                    break;
            }
        }
    }
}






