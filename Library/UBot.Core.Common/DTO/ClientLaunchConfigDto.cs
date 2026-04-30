namespace UBot.Core.Common.DTO;

public sealed class ClientLaunchConfigDto
{
    public string SilkroadDirectory { get; set; }
    public string SilkroadExecutable { get; set; }
    public string ExecutablePath { get; set; }
    public string ClientLibraryPath { get; set; }
    public string BasePath { get; set; }
    public string CommandLineArguments { get; set; }
    public string RuSroLogin { get; set; }
    public string RuSroPassword { get; set; }
    public byte ClientType { get; set; }
    public byte ContentId { get; set; }
    public byte DivisionIndex { get; set; }
    public byte GatewayIndex { get; set; }
    public bool RequiresXigncodePatch { get; set; }
    public bool LoaderDebugMode { get; set; }
    public ushort RedirectPort { get; set; }
    public ushort GatewayPort { get; set; }
    public string[] GatewayServers { get; set; } = [];
    public string[] SignatureFilePaths { get; set; } = [];
}
