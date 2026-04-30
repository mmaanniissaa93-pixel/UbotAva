using System.Threading.Tasks;
using UBot.Core.Common.DTO;

namespace UBot.Core.Abstractions.Services;

public interface IClientNativeRuntime
{
    bool IsRunning { get; }
    Task<bool> StartAsync(ClientLaunchConfigDto config, string xigncodeSignature);
    void Kill();
    void SetTitle(string title);
    void SetVisible(bool visible);
}
