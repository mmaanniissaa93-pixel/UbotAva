using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreServiceRuntimeEnvironment : IServiceRuntimeEnvironment
{
    public string BasePath => Kernel.BasePath;
}
