using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Inventory;

public class InventoryUpdateAmmoResponse : IPacketHandler
{
    public ushort Opcode => 0x3201;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var ammunitionAmount = packet.ReadUShort();
        dynamic player = UBot.Protocol.ProtocolRuntime.GameState?.Player;
        player?.Inventory.UpdateItemAmount(7, ammunitionAmount);

        UBot.Protocol.ProtocolRuntime.GameState?.FireEvent("OnUpdateAmmunition");
    }
}

