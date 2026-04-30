using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Network;
using UBot.Core.Abstractions.Services;

namespace UBot.Protocol.Services;

public sealed class ProtocolServices
{
    public ProtocolServices(
        IGameSession game,
        IKernelRuntime kernel,
        IPacketDispatcher packetDispatcher,
        IScriptEventBus eventBus,
        IGlobalSettings globalSettings)
    {
        Game = game;
        Kernel = kernel;
        PacketDispatcher = packetDispatcher;
        EventBus = eventBus;
        GlobalSettings = globalSettings;
    }

    public IGameSession Game { get; }
    public IKernelRuntime Kernel { get; }
    public IPacketDispatcher PacketDispatcher { get; }
    public IScriptEventBus EventBus { get; }
    public IGlobalSettings GlobalSettings { get; }
}
