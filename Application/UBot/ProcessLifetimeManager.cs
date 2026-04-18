using System;
using System.Runtime.InteropServices;
using UBot.Core;
using UBot.Core.Extensions;

namespace UBot;

internal static class ProcessLifetimeManager
{
    private static readonly object Sync = new();
    private static bool _initialized;
    private static IntPtr _jobHandle = IntPtr.Zero;

    public static void TryEnableChildProcessTerminationOnExit()
    {
        lock (Sync)
        {
            if (_initialized)
                return;

            _initialized = true;
        }

        try
        {
            _jobHandle = NativeExtensions.CreateJobObjectW(IntPtr.Zero, null);
            if (_jobHandle == IntPtr.Zero)
            {
                Log.Warn("Process lifetime manager: failed to create Job Object.");
                return;
            }

            var limits = new NativeExtensions.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new NativeExtensions.JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = NativeExtensions.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                }
            };

            var size = (uint)Marshal.SizeOf<NativeExtensions.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            var ptr = Marshal.AllocHGlobal((int)size);
            try
            {
                Marshal.StructureToPtr(limits, ptr, false);
                if (!NativeExtensions.SetInformationJobObject(
                        _jobHandle,
                        NativeExtensions.JobObjectExtendedLimitInformation,
                        ptr,
                        size))
                {
                    Log.Warn("Process lifetime manager: failed to configure Job Object limits.");
                    NativeExtensions.CloseHandle(_jobHandle);
                    _jobHandle = IntPtr.Zero;
                    return;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            if (!NativeExtensions.AssignProcessToJobObject(_jobHandle, NativeExtensions.GetCurrentProcess()))
            {
                Log.Warn("Process lifetime manager: failed to assign current process to Job Object.");
                NativeExtensions.CloseHandle(_jobHandle);
                _jobHandle = IntPtr.Zero;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Process lifetime manager failed: {ex.Message}");
            if (_jobHandle != IntPtr.Zero)
            {
                NativeExtensions.CloseHandle(_jobHandle);
                _jobHandle = IntPtr.Zero;
            }
        }
    }
}
