# Packet Replay Harness

`UBot.Core.Network.PacketReplayHarness` allows offline replay of packet traces through registered hooks/handlers.

## Trace format
Use JSON array:

```json
[
  {
    "destination": "Server",
    "opcode": "0x705A",
    "payload": "01 00 00 00 02 00 00 00",
    "encrypted": false,
    "massive": false
  }
]
```

Or wrapped object:

```json
{
  "packets": [
    {
      "destination": "Client",
      "opcode": "0x3026",
      "payload": "05 48 65 6C 6C 6F"
    }
  ]
}
```

## Usage
```csharp
var result = PacketReplayHarness.ReplayFromFile(@"C:\temp\packets.json");

Console.WriteLine($"Total={result.TotalPackets}, Replayed={result.ReplayedPackets}, Failed={result.FailedPackets}");
foreach (var error in result.Errors)
    Console.WriteLine(error);
```

## Notes
- `opcode` supports decimal or hex (`0x` prefix).
- `payload` accepts plain hex, with or without spaces.
- A packet dropped by hooks is counted as `FilteredPackets`.
