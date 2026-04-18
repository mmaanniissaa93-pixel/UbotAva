using UBot.Core.Event;
using UBot.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static UBot.Core.Extensions.NativeExtensions;

namespace UBot.Core.Components;

public partial class ClientManager
{
    private static Process _process;
    private static readonly string[] GitHubSignatureUrls =
    {
        "https://raw.githubusercontent.com/mmaanniissaa93-pixel/ubot-new/main/client-signatures.cfg",
        "https://raw.githubusercontent.com/mmaanniissaa93-pixel/ubot-new/master/client-signatures.cfg",
        "https://raw.githubusercontent.com/mmaanniissaa93-pixel/ubot/main/client-signatures.cfg",
        "https://raw.githubusercontent.com/mmaanniissaa93-pixel/ubot/master/client-signatures.cfg"
    };

    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// Get, has client exited <c>true</c> otherwise; <c>false</c>
    /// </summary>
    public static bool IsRunning => _process?.HasExited == false;

    /// <summary>
    /// Loads client signatures from GitHub repository
    /// </summary>
    private static async Task<Dictionary<GameClientType, string>> LoadSignaturesFromGitHub()
    {
        foreach (var url in GitHubSignatureUrls)
        {
            try
            {
                Log.Notify($"Fetching client signatures from GitHub: {url}");
                var response = await _httpClient.GetStringAsync(url);
                var signatures = ParseSignatures(response, $"GitHub ({url})");
                if (signatures.Count > 0)
                    return signatures;
            }
            catch (HttpRequestException ex)
            {
                Log.Error($"Failed to fetch signatures from GitHub ({url}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error loading signatures from GitHub ({url}): {ex.Message}");
            }
        }

        return [];
    }

    private static Dictionary<GameClientType, string> LoadSignaturesFromLocalFile()
    {
        var candidatePaths = new[]
        {
            Path.Combine(Kernel.BasePath ?? string.Empty, "client-signatures.cfg"),
            Path.Combine(AppContext.BaseDirectory, "client-signatures.cfg"),
            Path.Combine(Environment.CurrentDirectory, "client-signatures.cfg")
        };

        foreach (var path in candidatePaths
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
                Log.Warn($"Failed to load client signatures from local file ({path}): {ex.Message}");
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
                Log.Warn($"Unknown client type in signatures from {source}: {clientTypeName}");
            }
        }

        if (signatures.Count > 0)
            Log.Notify($"Successfully loaded {signatures.Count} client signatures from {source}");

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

    /// <summary>
    /// Gets the signature for the specified client type
    /// </summary>
    private static async Task<string> GetSignature(GameClientType type, Dictionary<GameClientType, string> signatures)
    {
        if (signatures.TryGetValue(type, out var signature))
            return signature;

        throw new ArgumentOutOfRangeException(nameof(type),
            $"No signature found for client type: {type}");
    }

    /// <summary>
    /// Start the game client
    /// </summary>
    /// <returns>Has successfully started <c>true</c>; otherwise <c>false</c></returns>
    public static async Task<bool> Start()
    {
        var silkroadDirectory = GlobalConfig.Get<string>("UBot.SilkroadDirectory");
        var executable = GlobalConfig.Get<string>("UBot.SilkroadExecutable");
        var path = Path.Combine(silkroadDirectory, executable);

        if (!File.Exists(path))
        {
            Log.Error($"Silkroad executable not found: {path}");
            return false;
        }

        var libraryDllName = "Client.Library.dll";
        var fullPath = Path.Combine(Kernel.BasePath, libraryDllName);

        if (!File.Exists(fullPath))
        {
            Log.Error($"Client library not found: {fullPath}");
            return false;
        }

        var buffer = Encoding.Unicode.GetBytes(fullPath + "\0");
        var pathLen = (uint)buffer.Length;

        var originalPathEnv = Environment.GetEnvironmentVariable("PATH");
        var launchPathEnv = BuildPathEnvironmentWith(Kernel.BasePath, originalPathEnv);
        var pathEnvChanged = !string.Equals(launchPathEnv, originalPathEnv, StringComparison.OrdinalIgnoreCase);
        if (pathEnvChanged)
            Environment.SetEnvironmentVariable("PATH", launchPathEnv);

        var gatewayIndex = GlobalConfig.Get<byte>("UBot.GatewayIndex");
        var divisionIndex = GlobalConfig.Get<byte>("UBot.DivisionIndex");
        var contentId = Game.ReferenceManager.DivisionInfo.Locale;

        var args = BuildCommandLineArguments(contentId, divisionIndex, gatewayIndex);

        var si = new STARTUPINFO();

        try
        {
            CreateMutex(0, false, "Silkroad Online Launcher");
            CreateMutex(0, false, "Ready");

            // Create suspended process
            if (!CreateProcess(
                null,
                $"\"{path}\" {args}",
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                CREATE_SUSPENDED,
                IntPtr.Zero,
                silkroadDirectory,
                ref si,
                out var pi))
            {
                Log.Error("Failed to create game process");
                return false;
            }

            var semaphore = new Semaphore(0, 1, pi.dwProcessId.ToString());

            try
            {
                PrepareTempConfigFile(pi.dwProcessId, divisionIndex);
                var mainThreadResumedForPatch = false;

                var sroProcess = Process.GetProcessById((int)pi.dwProcessId);

                if (RequiresXigncodePatch(Game.ClientType) && !await ApplyXigncodePatch(sroProcess, pi))
                    return false;
                if (RequiresXigncodePatch(Game.ClientType))
                {
                    var resumeResult = ResumeThread(pi.hThread);
                    if (resumeResult == uint.MaxValue)
                    {
                        Log.Warn("Failed to resume main thread after XIGNCODE patch; continuing with suspended state.");
                    }
                    else
                    {
                        mainThreadResumedForPatch = true;
                    }
                }

                _process = sroProcess;

                if (!InjectClientLibrary(pi, buffer, pathLen))
                {
                    CleanupProcess(pi);
                    return false;
                }

                if (!mainThreadResumedForPatch)
                    ResumeThread(pi.hThread);

                _process.Refresh();
                if (_process.HasExited)
                {
                    Log.Error($"Process exited immediately after start (exit code: 0x{_process.ExitCode:X})");
                    return false;
                }

                _process.EnableRaisingEvents = true;
                _process.Exited += ClientProcess_Exited;

                EventManager.FireEvent("OnStartClient");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start client: {ex.Message}");
                CleanupProcess(pi);
                return false;
            }
        }
        finally
        {
            if (pathEnvChanged)
                Environment.SetEnvironmentVariable("PATH", originalPathEnv);
        }
    }

    private static string BuildPathEnvironmentWith(string preferredPath, string currentPath)
    {
        if (string.IsNullOrWhiteSpace(preferredPath))
            return currentPath ?? string.Empty;

        var normalizedPreferredPath = preferredPath.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPreferredPath))
            return currentPath ?? string.Empty;

        var segments = (currentPath ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Trim())
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToList();

        if (segments.Any(segment => string.Equals(segment, normalizedPreferredPath, StringComparison.OrdinalIgnoreCase)))
            return currentPath ?? string.Empty;

        segments.Insert(0, normalizedPreferredPath);
        return string.Join(";", segments);
    }

    /// <summary>
    /// Builds command line arguments based on client type
    /// </summary>
    private static string BuildCommandLineArguments(byte contentId, byte divisionIndex, byte gatewayIndex)
    {
        if (Game.ClientType == GameClientType.RuSro)
        {
            var login = GlobalConfig.Get<string>("UBot.RuSro.login");
            var password = GlobalConfig.Get<string>("UBot.RuSro.password");
            return $"-LOGIN:{login} -PASSWORD:{password}";
        }

        return $"/{contentId} {divisionIndex} {gatewayIndex} 0";
    }

    /// <summary>
    /// Checks if the client type requires XIGNCODE patching
    /// </summary>
    private static bool RequiresXigncodePatch(GameClientType clientType)
    {
        return clientType == GameClientType.VTC_Game
            || clientType == GameClientType.Turkey
            || clientType == GameClientType.Taiwan;
    }

    /// <summary>
    /// Injects the client library into the target process
    /// </summary>
    private static bool InjectClientLibrary(PROCESS_INFORMATION pi, byte[] buffer, uint pathLen)
    {
        var handle = pi.hProcess;
        if (handle == IntPtr.Zero)
        {
            Log.Error("Process handle is invalid");
            return false;
        }

        try
        {
            var kernelHandle = GetModuleHandleW("kernel32.dll");
            if (kernelHandle == IntPtr.Zero)
            {
                Log.Error("Failed to get kernel32.dll handle");
                return false;
            }

            var loadLibAddr = GetProcAddress(kernelHandle, "LoadLibraryW");
            if (loadLibAddr == IntPtr.Zero)
            {
                Log.Error("Failed to get LoadLibraryW address");
                return false;
            }

            var remotePath = VirtualAllocEx(
                handle,
                IntPtr.Zero,
                pathLen,
                MEM_COMMIT | MEM_RESERVE,
                PAGE_READWRITE);

            if (remotePath == IntPtr.Zero)
            {
                Log.Error("Failed to allocate remote memory");
                return false;
            }

            try
            {
                if (!WriteProcessMemory(handle, remotePath, buffer, pathLen, out _))
                {
                    Log.Error("Failed to write library path to remote process");
                    return false;
                }

                var remoteThread = CreateRemoteThread(
                    handle,
                    IntPtr.Zero,
                    0,
                    loadLibAddr,
                    remotePath,
                    0,
                    IntPtr.Zero);

                if (remoteThread == IntPtr.Zero)
                {
                    Log.Error("Failed to create remote thread");
                    return false;
                }

                try
                {
                    Log.Debug("Waiting for LoadLibraryW to complete (10s timeout)...");
                    var waitResult = WaitForSingleObject(remoteThread, 10000);
                    if (waitResult != 0)
                    {
                        Log.Error("LoadLibraryW timed out after 10 seconds. The DLL injection may have deadlocked.");
                        return false;
                    }

                    if (!GetExitCodeThread(remoteThread, out var exitCode))
                    {
                        Log.Error("Failed to get remote thread exit code");
                        return false;
                    }

                    if (exitCode == 0)
                    {
                        Log.Error("LoadLibraryW failed: DLL could not be loaded. Verify the library exists and is compatible with the target process.");
                        return false;
                    }

                    // NTSTATUS error codes have the high two bits set (0xC0000000+)
                    if (exitCode >= 0xC0000000)
                    {
                        Log.Error($"LoadLibraryW crashed with NTSTATUS 0x{exitCode:X}. The DLL may be incompatible with the target process.");
                        return false;
                    }

                    Log.Notify($"Client library injected successfully (module handle: 0x{exitCode:X})");
                    return true;
                }
                finally
                {
                    CloseHandle(remoteThread);
                }
            }
            finally
            {
                VirtualFreeEx(handle, remotePath, 0, MEM_RELEASE);
            }
        }
        catch (Exception ex) {
            Log.Error($"DLL injection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Applies an in-memory patch to the XIGNCODE module of the specified process
    /// </summary>
    private static async Task<bool> ApplyXigncodePatch(Process process, PROCESS_INFORMATION pi)
    {
        try
        {
            var signatures = await LoadSignaturesFromGitHub();
            if (signatures.Count == 0)
                signatures = LoadSignaturesFromLocalFile();
            if (signatures.Count == 0)
            {
                signatures = BuildEmbeddedFallbackSignatures();
                Log.Warn("Using embedded fallback client signatures.");
            }

            if (signatures.Count == 0)
            {
                Log.Error("No client signatures loaded. Cannot start client.");
                return false;
            }

            ResumeThread(pi.hThread);
            await Task.Delay(250);
            SuspendThread(pi.hThread);

            var moduleMemory = new byte[process.MainModule.ModuleMemorySize];
            if (!ReadProcessMemory(
                process.Handle,
                process.MainModule.BaseAddress,
                moduleMemory,
                process.MainModule.ModuleMemorySize,
                out _))
            {
                Log.Error("Failed to read process memory for XIGNCODE patch");
                return false;
            }

            var signature = await GetSignature(Game.ClientType, signatures);
            var baseAddress = process.MainModule.BaseAddress.ToInt32();
            var address = FindPattern(signature, moduleMemory, baseAddress);

            if (address == IntPtr.Zero)
            {
                Log.Error("XIGNCODE patching failed! Signature not found.");
                Log.Error($"Please check if the signature for {Game.ClientType} is correct in the GitHub repository.");
                return false;
            }

            // Apply patches
            var patchJmp = new byte[] { 0xEB };
            var patchNop2 = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 };

            if (!WriteProcessMemory(pi.hProcess, address - 0x6F, patchJmp, 1, out _)
                || !WriteProcessMemory(pi.hProcess, address + 0x13, patchJmp, 1, out _)
                || !WriteProcessMemory(pi.hProcess, address + 0xC, patchNop2, 5, out _)
                || !WriteProcessMemory(pi.hProcess, address + 0x95, patchJmp, 1, out _))
            {
                Log.Error("XIGNCODE patching failed while writing memory.");
                return false;
            }

            Log.Notify("XIGNCODE patch applied successfully");
        }
        catch (Exception ex)
        {
            Log.Error($"XIGNCODE patching exception: {ex.Message}");
            return false;
        }
        finally
        {
            GC.Collect();
        }

        return true;
    }

    /// <summary>
    /// Kill the game client process
    /// </summary>
    public static void Kill()
    {
        var process = _process;
        if (process == null)
            return;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch (InvalidOperationException)
        {
            // process already exited
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to kill client process: {ex.Message}");
        }
        finally
        {
            try
            {
                process.Exited -= ClientProcess_Exited;
            }
            catch
            {
                // ignore
            }

            try
            {
                process.Dispose();
            }
            catch
            {
                // ignore
            }

            if (ReferenceEquals(_process, process))
                _process = null;
        }
    }

    /// <summary>
    /// Change client process title
    /// </summary>
    public static void SetTitle(string title)
    {
        if (_process != null && _process.MainWindowHandle != IntPtr.Zero)
            SetWindowText(_process.MainWindowHandle, title);
    }

    /// <summary>
    /// Change client visibility
    /// </summary>
    public static void SetVisible(bool visible)
    {
        if (_process != null && _process.MainWindowHandle != IntPtr.Zero)
            ShowWindow(_process.MainWindowHandle, visible ? SW_SHOW : SW_HIDE);
    }

    /// <summary>
    /// Handles client process exit event
    /// </summary>
    private static void ClientProcess_Exited(object sender, EventArgs e)
    {
        Log.Warn("Client process exited!");
        EventManager.FireEvent("OnExitClient");
    }

    /// <summary>
    /// Prepare the config file for loader
    /// </summary>
    private static void PrepareTempConfigFile(uint processId, int divisionIndex)
    {
        try
        {
            var tmpConfigFile = $"UBot_{processId}.tmp";
            var division = Game.ReferenceManager.DivisionInfo.Divisions[divisionIndex];
            var gatewayPort = Game.ReferenceManager.GatewayInfo.Port;
            var redirectIp = "127.0.0.1";

            using var writer = new BinaryWriter(
                new FileStream(
                    Path.Combine(Path.GetTempPath(), tmpConfigFile),
                    FileMode.Create));

            writer.Write(GlobalConfig.Get<bool>("UBot.Loader.DebugMode"));
            writer.WriteAscii(redirectIp);
            writer.Write(Kernel.Proxy.Port);
            writer.Write(division.GatewayServers.Count);

            foreach (var gatewayServer in division.GatewayServers)
                writer.WriteAscii(gatewayServer);

            writer.Write(gatewayPort);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to prepare temp config file: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches the specified buffer for the first occurrence of a byte pattern
    /// </summary>
    private static IntPtr FindPattern(string stringPattern, byte[] buffer, int baseAddress)
    {
        try
        {
            var pattern = stringPattern
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => byte.Parse(p, NumberStyles.AllowHexSpecifier))
                .ToArray();

            var patternLength = pattern.Length;
            var searchLength = buffer.Length - patternLength;

            for (var i = 0; i < searchLength; i++)
            {
                var found = true;
                for (var j = 0; j < patternLength; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                    return (IntPtr)(baseAddress + i);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Pattern search failed: {ex.Message}");
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Cleanup process handles
    /// </summary>
    private static void CleanupProcess(PROCESS_INFORMATION pi)
    {
        try
        {
            if (pi.hThread != IntPtr.Zero)
                CloseHandle(pi.hThread);

            if (pi.hProcess != IntPtr.Zero)
            {
                try
                {
                    var process = Process.GetProcessById((int)pi.dwProcessId);
                    process?.Kill();
                }
                catch (ArgumentException)
                {
                    // Process has already exited
                }

                CloseHandle(pi.hProcess);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to cleanup process: {ex.Message}");
        }
    }
}

