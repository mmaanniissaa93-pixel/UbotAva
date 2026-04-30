namespace UBot.Core.Bootstrap;

public sealed class BootstrapConfiguration
{
    public static BootstrapConfiguration Default { get; } = new();

    public bool ValidateOnBuild { get; init; }
}
