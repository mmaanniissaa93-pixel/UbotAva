using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.StorageBox;

public class StorageBoxTakeItemResponse : IPacketHandler
{
    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    public ushort Opcode => 0xB558;

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    /// <param name="packet"></param>
    public void Invoke(Packet packet)
    {
        var result = packet.ReadBool();
        if (!result)
            return;

        var count = packet.ReadInt();
        for (var i = 0; i < count; i++)
        {
            var item = packet.ReadInventoryItem();
            if (item == null)
                continue;

            CoreGame.Player.Inventory.Add(item);
        }

        EventManager.FireEvent("OnInventoryUpdate");
    }
}






