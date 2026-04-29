using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Network;

namespace UBot.Protocol;

public static class ProtocolRuntime
{
    public static IGameStateRuntimeContext GameState { get; set; }
    public static IPacketDispatcher PacketDispatcher { get; set; }
}
