using UBot.Core;
using UBot.Core.Network;

namespace UBot.AutoDungeon.Network;

internal class ForgottenWorldInvitationHandler : IPacketHandler
{
    public ushort Opcode => 0x751A;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        if (!AutoDungeonState.TryBuildForgottenWorldAcceptPacket(packet, out var response))
            return;

        UBot.Core.RuntimeAccess.Packets.SendPacket(response, PacketDestination.Server);
        Log.Notify("[AutoDungeon] Forgotten World invitation accepted automatically.");
    }
}
