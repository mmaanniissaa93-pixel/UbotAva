using System.Threading.Tasks;

namespace UBot.Core.Abstractions.Services;

public interface IClientLaunchPolicy
{
    Task<bool> StartAsync();
    bool RequiresXigncodePatch(GameClientType clientType);
}
