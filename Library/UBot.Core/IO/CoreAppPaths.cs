using UBot.Core.Abstractions.Services;
using KState = UBot.Core.Kernel;

namespace UBot.Core.IO;

internal sealed class CoreAppPaths : IAppPaths
{
    public string BasePath => KState.BasePath;
}
