using UBot.Core.Abstractions.Services;

namespace UBot.Core.IO;

internal sealed class CoreAppPaths : IAppPaths
{
    public string BasePath => Kernel.BasePath;
}
