using System;
using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Character;

public class CharacterIncreaseStrResponse : IPacketHandler
{
    public ushort Opcode => 0xB050;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        try
        {
            if (packet.ReadByte() != 1)
                return;

            dynamic player = UBot.Protocol.ProtocolRuntime.GameState?.Player;
            if (player == null)
                return;

            var oldStatPoints = (int)player.StatPoints;
            if (oldStatPoints <= 0)
            {
                Log.Debug("[CharacterIncreaseStrResponse] StatPoints underflow prevented: current=" + oldStatPoints);
                return;
            }

            player.StatPoints--;
            Log.Debug("[CharacterIncreaseStrResponse] StatPoints changed: old=" + oldStatPoints + " new=" + player.StatPoints + " reason=IncreaseStr");
            UBot.Protocol.ProtocolRuntime.GameState?.FireEvent("OnIncreaseStrength");
        }
        catch (Exception ex)
        {
            Log.Error("[CharacterIncreaseStrResponse] Exception in handler: " + ex.Message);
        }
    }
}

