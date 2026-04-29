using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.StorageBox;

public class StorageBoxTakeItemResponse
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






