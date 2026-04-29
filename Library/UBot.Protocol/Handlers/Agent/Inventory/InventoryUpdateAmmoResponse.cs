using UBot.Protocol;

namespace UBot.Core.Network.Handler.Agent.Inventory;

public class InventoryUpdateAmmoResponse : IPacketHandler
{
    public ushort Opcode => 0x3201;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var ammunitionAmount = packet.ReadUShort();
        dynamic player = ProtocolRuntime.GameState?.Player;
        player?.Inventory.UpdateItemAmount(7, ammunitionAmount);

        ProtocolRuntime.GameState?.FireEvent("OnUpdateAmmunition");
    }
}
