using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects;
using UBot.Core.Objects.Cos;

namespace UBot.Core.ProtocolLegacy.Handler.Agent;

internal class CosDataResponse
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x30C8;

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
        var objectId = packet.ReadUInt();

        var objChar = CoreGame.ReferenceManager.GetRefObjChar(objectId);
        if (objChar.TypeID2 == 2 && objChar.TypeID3 == 3)
        {
            var hp = packet.ReadInt();
            var maxHp = packet.ReadInt();
            maxHp = maxHp != 0 && maxHp != 200 ? maxHp : objChar.MaxHealth;

            switch (objChar.TypeID4)
            {
                case 1:

                    CoreGame.Player.Transport = new Transport
                    {
                        Id = objectId,
                        UniqueId = uniqueId,
                        Health = hp,
                        MaxHealth = maxHp,
                    };

                    CoreGame.Player.StopMoving();
                    CoreGame.Player.SetSpeed(objChar.Speed1, objChar.Speed2);

                    EventManager.FireEvent("OnSummonTransport");
                    EventManager.FireEvent("OnSummonCos", CoreGame.Player.Transport);

                    break;
                case 2:

                    CoreGame.Player.JobTransport = new JobTransport
                    {
                        Id = objectId,
                        UniqueId = uniqueId,
                        Health = hp,
                        MaxHealth = maxHp,
                        Inventory = packet.ReadInventoryItemCollection(),
                        OwnerUniqueId = packet.ReadUInt(),
                    };

                    EventManager.FireEvent("OnSummonJobTransport");
                    EventManager.FireEvent("OnSummonCos", CoreGame.Player.JobTransport);

                    CoreGame.Player.StopMoving();
                    CoreGame.Player.SetSpeed(objChar.Speed1, objChar.Speed2);

                    break;
                case 3:

                    CoreGame.Player.Growth = new Growth
                    {
                        Id = objectId,
                        UniqueId = uniqueId,
                        Health = hp,
                        MaxHealth = maxHp,
                    };
                    CoreGame.Player.Growth.Deserialize(packet);

                    EventManager.FireEvent("OnSummonGrowth");
                    EventManager.FireEvent("OnSummonCos", CoreGame.Player.Growth);

                    break;
                case 4:

                    CoreGame.Player.AbilityPet = new Ability
                    {
                        Id = objectId,
                        UniqueId = uniqueId,
                        Health = hp,
                        MaxHealth = maxHp,
                    };

                    CoreGame.Player.AbilityPet.Deserialize(packet);
                    EventManager.FireEvent("OnSummonCos", CoreGame.Player.AbilityPet);
                    EventManager.FireEvent("OnSummonAbility");

                    break;
                case 9:

                    CoreGame.Player.Fellow = new Fellow
                    {
                        Id = objectId,
                        UniqueId = uniqueId,
                        Health = hp,
                        MaxHealth = maxHp,
                    };

                    CoreGame.Player.Fellow.Deserialize(packet);
                    EventManager.FireEvent("OnSummonCos", CoreGame.Player.Fellow);

                    break;
            }
        }
    }
}






