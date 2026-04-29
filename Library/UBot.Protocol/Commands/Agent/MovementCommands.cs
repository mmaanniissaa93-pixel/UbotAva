using UBot.Core.Network;

namespace UBot.Protocol.Commands.Agent;

public static class MovementCommands
{
    public static Packet BuildStopMovement()
    {
        return new Packet(0x7021);
    }

    public static Packet BuildEnterBerzerkMode()
    {
        return new Packet(0x70A7);
    }
}
