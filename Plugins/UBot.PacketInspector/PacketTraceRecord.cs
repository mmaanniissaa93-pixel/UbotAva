using System;
using UBot.Core.Network;

namespace UBot.PacketInspector;

internal sealed class PacketTraceRecord
{
    public DateTime Timestamp { get; init; }
    public PacketDestination Destination { get; init; }
    public ushort Opcode { get; init; }
    public int OriginalLength { get; init; }
    public byte[] Payload { get; init; } = [];
}
