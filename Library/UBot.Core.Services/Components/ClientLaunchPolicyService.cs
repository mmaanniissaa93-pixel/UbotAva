using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Services;
using UBot.Core.Common.DTO;
using UBot.Core.Services;

namespace UBot.Core.Components;

public sealed class ClientLaunchPolicyService : IClientLaunchPolicy
{
    private static readonly string[] GitHubSignatureUrls =
    {
        "https://raw.githubusercontent.com/mmaanniissaa93-pixel/ubot-new/main/client-signatures.cfg",
        "https://raw.githubusercontent.com/mmaanniissaa93-pixel/ubot-new/master/client-signatures.cfg",
        "https://raw.githubusercontent.com/mmaanniissaa93-pixel/ubot/main/client-signatures.cfg",
        "https://raw.githubusercontent.com/mmaanniissaa93-pixel/ubot/master/client-signatures.cfg"
    };

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<bool> StartAsync()
    {
        var config = ConfigProvider?.Load();
        if (config == null)
        {
            ServiceRuntime.Log?.Warn("Client launch config could not be loaded.");
            return false;
        }

        var clientType = (GameClientType)config.ClientType;
        config.RequiresXigncodePatch = RequiresXigncodePatch(clientType);
        config.CommandLineArguments = BuildCommandLineArguments(config, clientType);

        var signature = string.Empty;
        if (config.RequiresXigncodePatch)
        {
            signature = await ResolveSignatureAsync(clientType, config).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(signature))
            {
                ServiceRuntime.Log?.Warn("No client signatures loaded. Cannot start client.");
                return false;
            }
        }

        if (Runtime == null)
        {
            ServiceRuntime.Log?.Warn("Client native runtime is not registered.");
            return false;
        }

        return await Runtime.StartAsync(config, signature).ConfigureAwait(false);
    }

    public bool RequiresXigncodePatch(GameClientType clientType)
    {
        return clientType == GameClientType.VTC_Game
            || clientType == GameClientType.Turkey
            || clientType == GameClientType.Taiwan;
    }

    private static string BuildCommandLineArguments(ClientLaunchConfigDto config, GameClientType clientType)
    {
        if (clientType == GameClientType.RuSro)
            return $"-LOGIN:{config.RuSroLogin} -PASSWORD:{config.RuSroPassword}";

        return $"/{config.ContentId} {config.DivisionIndex} {config.GatewayIndex} 0";
    }

    private static async Task<string> ResolveSignatureAsync(GameClientType clientType, ClientLaunchConfigDto config)
    {
        var signatures = await LoadSignaturesFromGitHubAsync().ConfigureAwait(false);
        if (signatures.Count == 0)
            signatures = LoadSignaturesFromLocalFile(config);

        if (signatures.Count == 0)
        {
            signatures = BuildEmbeddedFallbackSignatures();
            ServiceRuntime.Log?.Warn("Using embedded fallback client signatures.");
        }

        if (signatures.TryGetValue(clientType, out var signature))
            return signature;

        ServiceRuntime.Log?.Warn($"No signature found for client type: {clientType}");
        return string.Empty;
    }

    private static async Task<Dictionary<GameClientType, string>> LoadSignaturesFromGitHubAsync()
    {
        foreach (var url in GitHubSignatureUrls)
        {
            try
            {
                ServiceRuntime.Log?.Notify($"Fetching client signatures from GitHub: {url}");
                var response = await HttpClient.GetStringAsync(url).ConfigureAwait(false);
                var signatures = ParseSignatures(response, $"GitHub ({url})");
                if (signatures.Count > 0)
                    return signatures;
            }
            catch (HttpRequestException ex)
            {
                ServiceRuntime.Log?.Warn($"Failed to fetch signatures from GitHub ({url}): {ex.Message}");
            }
            catch (Exception ex)
            {
                ServiceRuntime.Log?.Warn($"Unexpected error loading signatures from GitHub ({url}): {ex.Message}");
            }
        }

        return [];
    }

    private static Dictionary<GameClientType, string> LoadSignaturesFromLocalFile(ClientLaunchConfigDto config)
    {
        foreach (var path in (config.SignatureFilePaths ?? [])
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                var content = File.ReadAllText(path);
                var signatures = ParseSignatures(content, $"local file ({path})");
                if (signatures.Count > 0)
                    return signatures;
            }
            catch (Exception ex)
            {
                ServiceRuntime.Log?.Warn($"Failed to load client signatures from local file ({path}): {ex.Message}");
            }
        }

        return [];
    }

    private static Dictionary<GameClientType, string> ParseSignatures(string content, string source)
    {
        var signatures = new Dictionary<GameClientType, string>();
        foreach (var line in content.Split('\n'))
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine)
                || trimmedLine.StartsWith("#")
                || trimmedLine.StartsWith("//"))
            {
                continue;
            }

            var parts = trimmedLine.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var clientTypeName = parts[0].Trim();
            var signature = parts[1].Replace('"', ' ').Trim();
            if (string.IsNullOrWhiteSpace(signature))
                continue;

            if (Enum.TryParse<GameClientType>(clientTypeName, true, out var clientType))
            {
                signatures[clientType] = signature;
            }
            else
            {
                ServiceRuntime.Log?.Warn($"Unknown client type in signatures from {source}: {clientTypeName}");
            }
        }

        if (signatures.Count > 0)
            ServiceRuntime.Log?.Notify($"Successfully loaded {signatures.Count} client signatures from {source}");

        return signatures;
    }

    private static Dictionary<GameClientType, string> BuildEmbeddedFallbackSignatures()
    {
        return new Dictionary<GameClientType, string>
        {
            [GameClientType.Turkey] = "6A 00 68 78 18 43 01 68 8C 18 43 01",
            [GameClientType.VTC_Game] = "6A 00 68 F8 91 3F 01 68 0C 92 3F 01",
            [GameClientType.Taiwan] = "6A 00 68 30 58 43 01 68 44 58 43 01"
        };
    }

    private static IClientLaunchConfigProvider ConfigProvider => ServiceRuntime.ClientLaunchConfigProvider;
    private static IClientNativeRuntime Runtime => ServiceRuntime.ClientNativeRuntime;
}
