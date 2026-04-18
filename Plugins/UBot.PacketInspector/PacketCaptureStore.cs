using System;
using System.Collections.Generic;
using System.Linq;
using UBot.Core.Network;

namespace UBot.PacketInspector;

internal static class PacketCaptureStore
{
    private static readonly object _lock = new();
    private static readonly List<PacketTraceRecord> _records = [];
    private static long _version;

    public static bool CaptureEnabled { get; set; } = false;
    public static int MaxEntries { get; set; } = 2000;
    public static int MaxPayloadBytes { get; set; } = 256;

    public static void Capture(Packet packet, PacketDestination destination)
    {
        if (!CaptureEnabled || packet == null)
            return;

        try
        {
            var bytes = packet.GetBytes() ?? [];
            var payload = bytes.Take(MaxPayloadBytes).ToArray();
            var entry = new PacketTraceRecord
            {
                Timestamp = DateTime.Now,
                Destination = destination,
                Opcode = packet.Opcode,
                OriginalLength = bytes.Length,
                Payload = payload
            };

            lock (_lock)
            {
                _records.Add(entry);

                if (_records.Count > MaxEntries)
                    _records.RemoveRange(0, _records.Count - MaxEntries);

                _version++;
            }
        }
        catch
        {
            // Ignore capture errors to keep network processing stable.
        }
    }

    public static (long Version, List<PacketTraceRecord> Records) Snapshot()
    {
        lock (_lock)
            return (_version, [.. _records]);
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _records.Clear();
            _version++;
        }
    }
}
