using UBot.Core.Common.DTO;

namespace UBot.Core.Abstractions.Services;

public interface IClientLaunchConfigProvider
{
    ClientLaunchConfigDto Load();
}
