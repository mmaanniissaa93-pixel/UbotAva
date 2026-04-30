using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.Common.DTO;
using UBot.Core.Event;
using UBot.Core.Extensions;
using static UBot.Core.Extensions.NativeExtensions;

namespace UBot.Core.ProtocolServices;

internal sealed class ClientNativeRuntimeAdapter : IClientNativeRuntime
{
    private Process _process;

    public bool IsRunning => _process?.HasExited == false;

    public async Task<bool> StartAsync(ClientLaunchConfigDto config, string xigncodeSignature)
    {
        if (!File.Exists(config.ExecutablePath))
        {
            Log.Error($"Silkroad executable not found: {config.ExecutablePath}");
            return false;
        }

        if (!File.Exists(config.ClientLibraryPath))
        {
            Log.Error($"Client library not found: {config.ClientLibraryPath}");
            return false;
        }

        var buffer = Encoding.Unicode.GetBytes(config.ClientLibraryPath + "\0");
        var pathLen = (uint)buffer.Length;

        var originalPathEnv = Environment.GetEnvironmentVariable("PATH");
        var launchPathEnv = BuildPathEnvironmentWith(config.BasePath, originalPathEnv);
        var pathEnvChanged = !string.Equals(launchPathEnv, originalPathEnv, StringComparison.OrdinalIgnoreCase);
        if (pathEnvChanged)
            Environment.SetEnvironmentVariable("PATH", launchPathEnv);

        var si = new STARTUPINFO();

        try
        {
            CreateMutex(0, false, "Silkroad Online Launcher");
            CreateMutex(0, false, "Ready");

            // Core-owned native boundary: process creation stays here.
            if (!CreateProcess(
                null,
                $"\"{config.ExecutablePath}\" {config.CommandLineArguments}",
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                CREATE_SUSPENDED,
                IntPtr.Zero,
                config.SilkroadDirectory,
                ref si,
                out var pi))
            {
                Log.Error("Failed to create game process");
                return false;
            }

            try
            {
                PrepareTempConfigFile(pi.dwProcessId, config);
                var mainThreadResumedForPatch = false;
                var sroProcess = Process.GetProcessById((int)pi.dwProcessId);

                // Core-owned native boundary: XIGNCODE patch writes target process memory.
                if (config.RequiresXigncodePatch && !await ApplyXigncodePatch(sroProcess, pi, xigncodeSignature).ConfigureAwait(false))
                    return false;

                if (config.RequiresXigncodePatch)
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

                // Core-owned native boundary: DLL injection writes and executes remote memory.
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

    public void Kill()
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
            }

            try
            {
                process.Dispose();
            }
            catch
            {
            }

            if (ReferenceEquals(_process, process))
                _process = null;
        }
    }

    public void SetTitle(string title)
    {
        if (_process != null && _process.MainWindowHandle != IntPtr.Zero)
            SetWindowText(_process.MainWindowHandle, title);
    }

    public void SetVisible(bool visible)
    {
        if (_process != null && _process.MainWindowHandle != IntPtr.Zero)
            ShowWindow(_process.MainWindowHandle, visible ? SW_SHOW : SW_HIDE);
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
        catch (Exception ex)
        {
            Log.Error($"DLL injection failed: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> ApplyXigncodePatch(Process process, PROCESS_INFORMATION pi, string signature)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                Log.Error("XIGNCODE patching failed! Signature is empty.");
                return false;
            }

            ResumeThread(pi.hThread);

            var moduleLoadDeadline = DateTime.UtcNow.AddSeconds(10);
            process.Refresh();
            while (process.MainModule == null && DateTime.UtcNow < moduleLoadDeadline)
            {
                await Task.Delay(50).ConfigureAwait(false);
                process.Refresh();
            }

            SuspendThread(pi.hThread);

            if (process.MainModule == null)
            {
                Log.Error("Process main module did not load within 10 seconds. Cannot apply XIGNCODE patch.");
                return false;
            }

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

            var baseAddress = process.MainModule.BaseAddress.ToInt32();
            var address = FindPattern(signature, moduleMemory, baseAddress);

            if (address == IntPtr.Zero)
            {
                Log.Error("XIGNCODE patching failed! Signature not found.");
                return false;
            }

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

    private static void ClientProcess_Exited(object sender, EventArgs e)
    {
        Log.Warn("Client process exited!");
        EventManager.FireEvent("OnExitClient");
    }

    private static void PrepareTempConfigFile(uint processId, ClientLaunchConfigDto config)
    {
        try
        {
            var tmpConfigFile = $"UBot_{processId}.tmp";
            var redirectIp = "127.0.0.1";

            using var writer = new BinaryWriter(
                new FileStream(
                    Path.Combine(Path.GetTempPath(), tmpConfigFile),
                    FileMode.Create));

            writer.Write(config.LoaderDebugMode);
            writer.WriteAscii(redirectIp);
            writer.Write(config.RedirectPort);
            writer.Write(config.GatewayServers.Length);

            foreach (var gatewayServer in config.GatewayServers)
                writer.WriteAscii(gatewayServer);

            writer.Write(config.GatewayPort);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to prepare temp config file: {ex.Message}");
        }
    }

    private static IntPtr FindPattern(string stringPattern, byte[] buffer, int baseAddress)
    {
        try
        {
            var tokens = stringPattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var pattern = tokens
                .Select(p => p == "??" ? (byte?)null : byte.Parse(p, NumberStyles.AllowHexSpecifier))
                .ToArray();

            var patternLength = pattern.Length;
            var searchLength = buffer.Length - patternLength;

            for (var i = 0; i < searchLength; i++)
            {
                var found = true;
                for (var j = 0; j < patternLength; j++)
                {
                    if (pattern[j].HasValue && buffer[i + j] != pattern[j].Value)
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
