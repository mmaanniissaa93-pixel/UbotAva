using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Network;
using UBot.Core.Abstractions.Services;

namespace UBot.Core.Services;

public static class ServiceRuntime
{
    public static IGameStateRuntimeContext GameState { get; set; }
    public static IPacketDispatcher PacketDispatcher { get; set; }
    public static IServiceRuntimeEnvironment Environment { get; set; }
    public static IServiceLog Log { get; set; }
}
