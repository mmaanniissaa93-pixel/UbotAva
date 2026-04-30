using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Protocol;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Objects.Inventory;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Protocol.Hooks.Agent.Inventory;

public class BuyItemHook : IPacketHook 
{
    /// <summary>
    ///     Gets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB034;

    /// <summary>
    ///     Gets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Replaces the packet and returns a new packet.
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
    public Packet ReplacePacket(Packet packet)
    {
        if (packet.ReadByte() != 0x01 || !ShoppingManager.Running)
            return packet;

        var type = (InventoryOperation)packet.ReadByte();
        if (type == InventoryOperation.SP_SELL_ITEM_COS)
        {
            var tempCosId = packet.ReadUInt(); //COS unique ID
            var srcSlot = packet.ReadByte();
            var response = new Packet(0xB034);
            response.WriteByte(1); //Success
            response.WriteByte(InventoryOperation.SP_DROP_ITEM_COS);
            response.WriteUInt(tempCosId);
            response.WriteByte(srcSlot);

            return response;
        }

        var cosId = 0u;
        if (type == InventoryOperation.SP_BUY_ITEM_COS)
            cosId = packet.ReadUInt();

        if (type != InventoryOperation.SP_BUY_ITEM && type != InventoryOperation.SP_BUY_ITEM_COS)
            return packet;

        var tab = packet.ReadByte();
        var slot = packet.ReadByte();

        byte[] destinationSlots = null;
        ushort amount = 0;
        byte itemAmount = 0;
        if (CoreGame.ClientType >= GameClientType.Chinese && CoreGame.ClientType != GameClientType.Rigid)
        {
            amount = packet.ReadUShort();
            itemAmount = packet.ReadByte();
            destinationSlots = packet.ReadBytes(itemAmount);
        }
        else
        {
            itemAmount = packet.ReadByte();
            destinationSlots = packet.ReadBytes(itemAmount);
            amount = packet.ReadUShort();
        }

        var bionic = CoreGame.SelectedEntity;
        if (bionic == null)
            return packet;

        var refPackageItem = CoreGame.ReferenceManager.GetRefPackageItem(bionic.Record.CodeName, tab, slot);
        var refItem = CoreGame.ReferenceManager.GetRefItem(refPackageItem.RefItemCodeName);

        if (refItem == null)
        {
            Log.Notify("You bought an item without any reference...");
            return packet;
        }

        if (itemAmount <= 0)
            itemAmount = 1;

        for (var i = 0; i < itemAmount; i++)
        {
            var destination = destinationSlots[i];

            var response = new Packet(0xB034);
            response.WriteByte(0x01); //Success

            if (cosId == 0)
            {
                response.WriteByte(InventoryOperation.SP_PICK_ITEM);
            }
            else
            {
                if (CoreGame.Player.JobTransport == null || cosId != CoreGame.Player.JobTransport.UniqueId)
                    return packet;

                response.WriteByte(InventoryOperation.SP_PICK_ITEM_COS);
                response.WriteUInt(cosId);
            }

            response.WriteByte(destination);

            if (CoreGame.ClientType > GameClientType.Thailand)
                response.WriteInt(0);

            response.WriteInt(refItem.ID);

            switch (refItem.TypeID2)
            {
                case 1:
                case 4:
                case 5:
                    response.WriteByte(refPackageItem.OptLevel);
                    response.WriteLong(refPackageItem.Variance);
                    response.WriteUInt(refPackageItem.Data);

                    response.WriteByte(0); //No magic options!

                    if (CoreGame.ClientType > GameClientType.Thailand)
                    {
                        var bindingCount = 2;
                        switch (CoreGame.ClientType)
                        {
                            case GameClientType.Chinese_Old:
                            case GameClientType.Chinese:
                            case GameClientType.Global:
                            case GameClientType.Turkey:
                            case GameClientType.Rigid:
                            case GameClientType.RuSro:
                            case GameClientType.Japanese:
                            case GameClientType.Taiwan:
                                bindingCount = 4;
                                break;
                            case GameClientType.VTC_Game:
                            case GameClientType.Korean:
                                bindingCount = 3;
                                break;
                        }

                        for (var j = 1; j <= bindingCount; j++)
                            response.WriteShort(j);
                    }

                    break;

                case 2: //ITEM_COS
                    switch (refItem.TypeID3)
                    {
                        case 1:
                            response.WriteByte(1); //State
                            break;

                        case 2:
                            packet.WriteUInt(0); //Monster mask or so
                            break;

                        case 3:
                            packet.WriteUShort(0); //Monster mask or so
                            break;

                        default:
                            if (refItem.TypeID4 == 3) //Magic cube
                                packet.WriteInt(amount);
                            break;
                    }

                    break;

                default: //ITEM_ETC
                    response.WriteUShort(amount);

                    if (refItem.TypeID3 == 11) //Magic stones
                        if (refItem.TypeID4 == 1 || refItem.TypeID4 == 2)
                            packet.WriteByte(0); //AttributeAssimilationProbability
                        else if (refItem.TypeID3 == 14 && refItem.TypeID4 == 2) //ITEM_MALL_GACHA_CARD_WIN & LOSE
                            packet.WriteByte(0);
                    break;
            }

            if (cosId != 0)
                response.WriteString(CoreGame.Player.Name); //OwnerName
            if (itemAmount > 1)
                ProtocolRuntime.Dispatch(response, Destination);
            else
                return response;
        }

        return null;
    }
}





