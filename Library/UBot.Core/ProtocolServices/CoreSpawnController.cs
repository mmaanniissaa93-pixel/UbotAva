using System;
using System.Linq;
using UBot.Core.Abstractions.Services;
using UBot.Core.Components;
using UBot.Core.Network;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreSpawnController : ISpawnController
{
    private readonly IScriptEventBus _events;
    private readonly IUIFeedbackService _feedback;
    private Packet _groupPacket;
    private ushort _groupAmount;
    private byte _groupType;

    public CoreSpawnController(IScriptEventBus events, IUIFeedbackService feedback)
    {
        _events = events;
        _feedback = feedback;
    }

    public object GetEntity(uint uniqueId)
    {
        return SpawnManager.GetEntity<SpawnedEntity>(uniqueId);
    }

    public object FindEntity(Func<object, bool> predicate)
    {
        return SpawnManager.TryGetEntities<SpawnedEntity>(entity => predicate(entity), out var entities)
            ? entities.FirstOrDefault()
            : null;
    }

    public void Parse(object packet, bool isGroup = false)
    {
        if (packet is Packet networkPacket)
            SpawnManager.Parse(networkPacket, isGroup);
    }

    public void BeginGroup(byte type, ushort amount)
    {
        _groupPacket = new Packet(0x3019);
        _groupType = type;
        _groupAmount = amount;
    }

    public void AppendGroupData(byte[] packetBytes)
    {
        _groupPacket?.WriteBytes(packetBytes);
    }

    public void EndGroup()
    {
        if (_groupPacket == null)
            return;

        var packet = _groupPacket;
        packet.Lock();

        for (var i = 0; i < _groupAmount; i++)
        {
            try
            {
                switch (_groupType)
                {
                    case 0x01:
                        SpawnManager.Parse(packet, true);
                        break;

                    case 0x02:
                        Despawn(packet.ReadUInt());
                        break;
                }
            }
            catch (Exception)
            {
                _feedback.Debug($"Spawn parse failed at index {i}!");
                break;
            }
        }

        _groupPacket = null;
        _groupAmount = 0;
        _groupType = 0;
    }

    public void Despawn(uint uniqueId)
    {
        var player = SpawnManager.GetEntity<SpawnedPlayer>(e => e.TransportUniqueId == uniqueId);

        if (player != null)
        {
            player.OnTransport = false;
            player.TransportUniqueId = 0;
        }

        SpawnManager.TryRemove(uniqueId, out var removedEntity);
        _events.Fire("OnDespawnEntity", removedEntity);
    }
}

