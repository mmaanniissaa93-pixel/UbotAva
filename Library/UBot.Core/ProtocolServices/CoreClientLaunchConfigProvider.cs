using System;
using System.IO;
using System.Linq;
using UBot.Core.Abstractions.Services;
using UBot.Core.Common.DTO;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreClientLaunchConfigProvider : IClientLaunchConfigProvider
{
    public ClientLaunchConfigDto Load()
    {
        var silkroadDirectory = GlobalConfig.Get<string>("UBot.SilkroadDirectory");
        var executable = GlobalConfig.Get<string>("UBot.SilkroadExecutable");
        var divisionIndex = GlobalConfig.Get<byte>("UBot.DivisionIndex");
        var gatewayIndex = GlobalConfig.Get<byte>("UBot.GatewayIndex");
        var division = Game.ReferenceManager.DivisionInfo.Divisions[divisionIndex];

        return new ClientLaunchConfigDto
        {
            SilkroadDirectory = silkroadDirectory,
            SilkroadExecutable = executable,
            ExecutablePath = Path.Combine(silkroadDirectory, executable),
            ClientLibraryPath = Path.Combine(Kernel.BasePath, "Client.Library.dll"),
            BasePath = Kernel.BasePath,
            ClientType = (byte)Game.ClientType,
            ContentId = Game.ReferenceManager.DivisionInfo.Locale,
            DivisionIndex = divisionIndex,
            GatewayIndex = gatewayIndex,
            RuSroLogin = GlobalConfig.Get<string>("UBot.RuSro.login"),
            RuSroPassword = GlobalConfig.Get<string>("UBot.RuSro.password"),
            LoaderDebugMode = GlobalConfig.Get<bool>("UBot.Loader.DebugMode"),
            RedirectPort = (ushort)Kernel.Proxy.Port,
            GatewayPort = (ushort)Game.ReferenceManager.GatewayInfo.Port,
            GatewayServers = division.GatewayServers.ToArray(),
            SignatureFilePaths =
            [
                Path.Combine(Kernel.BasePath ?? string.Empty, "client-signatures.cfg"),
                Path.Combine(AppContext.BaseDirectory, "client-signatures.cfg"),
                Path.Combine(Environment.CurrentDirectory, "client-signatures.cfg")
            ]
        };
    }
}
