using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace UBot.Core.Network;

public sealed class PacketReplayEntry
{
    public string Destination { get; set; } = PacketDestination.Server.ToString();
    public string Opcode { get; set; }
    public string Payload { get; set; } = string.Empty;
    public bool Encrypted { get; set; }
    public bool Massive { get; set; }
}

public sealed class PacketReplayResult
{
    public int TotalPackets { get; set; }
    public int ReplayedPackets { get; set; }
    public int FilteredPackets { get; set; }
    public int FailedPackets { get; set; }
    public List<string> Errors { get; } = [];
}

internal sealed class PacketReplayContainer
{
    public List<PacketReplayEntry> Packets { get; set; } = [];
}

public static class PacketReplayHarness
{
    public static PacketReplayResult ReplayFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new PacketReplayResult
            {
                TotalPackets = 0,
                FailedPackets = 1,
                Errors = { $"Replay file does not exist: {filePath}" }
            };

        var json = File.ReadAllText(filePath);
        var entries = ParseEntries(json);
        return Replay(entries);
    }

    public static PacketReplayResult Replay(IEnumerable<PacketReplayEntry> entries)
    {
        var result = new PacketReplayResult();
        if (entries == null)
            return result;

        var replayEntries = entries.ToList();
        result.TotalPackets = replayEntries.Count;

        for (var index = 0; index < replayEntries.Count; index++)
        {
            var entry = replayEntries[index];
            try
            {
                if (!TryParseDestination(entry.Destination, out var destination))
                {
                    result.FailedPackets++;
                    result.Errors.Add($"Line {index + 1}: Invalid destination '{entry.Destination}'.");
                    continue;
                }

                if (!TryParseOpcode(entry.Opcode, out var opcode))
                {
                    result.FailedPackets++;
                    result.Errors.Add($"Line {index + 1}: Invalid opcode '{entry.Opcode}'.");
                    continue;
                }

                if (!TryParsePayload(entry.Payload, out var payload))
                {
                    result.FailedPackets++;
                    result.Errors.Add($"Line {index + 1}: Invalid payload format.");
                    continue;
                }

                var packet = new Packet(
                    opcode,
                    entry.Encrypted,
                    entry.Massive,
                    payload,
                    0,
                    payload.Length,
                    locked: true
                );
                packet = PacketManager.CallHook(packet, destination);

                if (packet == null)
                {
                    result.FilteredPackets++;
                    continue;
                }

                packet.SeekRead(0, SeekOrigin.Begin);
                PacketManager.CallHandler(packet, destination);
                PacketManager.CallCallback(packet);

                result.ReplayedPackets++;
            }
            catch (Exception ex)
            {
                result.FailedPackets++;
                result.Errors.Add($"Line {index + 1}: {ex.Message}");
            }
        }

        return result;
    }

    private static List<PacketReplayEntry> ParseEntries(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var directList = JsonSerializer.Deserialize<List<PacketReplayEntry>>(json, options);
        if (directList != null)
            return directList;

        var wrapped = JsonSerializer.Deserialize<PacketReplayContainer>(json, options);
        return wrapped?.Packets ?? [];
    }

    private static bool TryParseDestination(string destinationText, out PacketDestination destination)
    {
        if (Enum.TryParse(destinationText, true, out destination))
            return true;

        destination = default;
        return false;
    }

    private static bool TryParseOpcode(string opcodeText, out ushort opcode)
    {
        opcode = 0;
        if (string.IsNullOrWhiteSpace(opcodeText))
            return false;

        var normalized = opcodeText.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ushort.TryParse(
                normalized[2..],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out opcode
            );

        return ushort.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out opcode);
    }

    private static bool TryParsePayload(string payloadText, out byte[] payload)
    {
        payload = [];

        if (string.IsNullOrWhiteSpace(payloadText))
            return true;

        var normalized = new string(payloadText.Where(Uri.IsHexDigit).ToArray());
        if (normalized.Length == 0)
            return true;

        if (normalized.Length % 2 != 0)
            return false;

        payload = new byte[normalized.Length / 2];
        for (var i = 0; i < normalized.Length; i += 2)
        {
            if (
                !byte.TryParse(
                    normalized.Substring(i, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out payload[i / 2]
                )
            )
                return false;
        }

        return true;
    }
}
