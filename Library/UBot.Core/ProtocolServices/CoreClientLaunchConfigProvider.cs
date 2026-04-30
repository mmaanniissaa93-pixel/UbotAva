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
        var silkroadDirectory = UBot.Core.RuntimeAccess.Global.Get<string>("UBot.SilkroadDirectory");
        var executable = UBot.Core.RuntimeAccess.Global.Get<string>("UBot.SilkroadExecutable");
        var divisionIndex = UBot.Core.RuntimeAccess.Global.Get<byte>("UBot.DivisionIndex");
        var gatewayIndex = UBot.Core.RuntimeAccess.Global.Get<byte>("UBot.GatewayIndex");
        var division = UBot.Core.RuntimeAccess.Session.ReferenceManager.DivisionInfo.Divisions[divisionIndex];

        return new ClientLaunchConfigDto
        {
            SilkroadDirectory = silkroadDirectory,
            SilkroadExecutable = executable,
            ExecutablePath = Path.Combine(silkroadDirectory, executable),
            ClientLibraryPath = Path.Combine(UBot.Core.RuntimeAccess.Core.BasePath, "Client.Library.dll"),
            BasePath = UBot.Core.RuntimeAccess.Core.BasePath,
            ClientType = (byte)UBot.Core.RuntimeAccess.Session.ClientType,
            ContentId = UBot.Core.RuntimeAccess.Session.ReferenceManager.DivisionInfo.Locale,
            DivisionIndex = divisionIndex,
            GatewayIndex = gatewayIndex,
            RuSroLogin = UBot.Core.RuntimeAccess.Global.Get<string>("UBot.RuSro.login"),
            RuSroPassword = UBot.Core.RuntimeAccess.Global.Get<string>("UBot.RuSro.password"),
            LoaderDebugMode = UBot.Core.RuntimeAccess.Global.Get<bool>("UBot.Loader.DebugMode"),
            RedirectPort = (ushort)UBot.Core.RuntimeAccess.Core.Proxy.Port,
            GatewayPort = (ushort)UBot.Core.RuntimeAccess.Session.ReferenceManager.GatewayInfo.Port,
            GatewayServers = division.GatewayServers.ToArray(),
            SignatureFilePaths =
            [
                Path.Combine(UBot.Core.RuntimeAccess.Core.BasePath ?? string.Empty, "client-signatures.cfg"),
                Path.Combine(AppContext.BaseDirectory, "client-signatures.cfg"),
                Path.Combine(Environment.CurrentDirectory, "client-signatures.cfg")
            ]
        };
    }
}
