using System;
using UBot.Core;
using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Character;

public class CharacterIncreaseIntResponse : IPacketHandler
{
    public ushort Opcode => 0xB051;

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
                Log.Debug("[CharacterIncreaseIntResponse] StatPoints underflow prevented: current=" + oldStatPoints);
                return;
            }

            player.StatPoints--;
            Log.Debug("[CharacterIncreaseIntResponse] StatPoints changed: old=" + oldStatPoints + " new=" + player.StatPoints + " reason=IncreaseInt");
            UBot.Protocol.ProtocolRuntime.GameState?.FireEvent("OnIncreaseIntelligence");
        }
        catch (Exception ex)
        {
            Log.Error("[CharacterIncreaseIntResponse] Exception in handler: " + ex.Message);
        }
    }
}

