using UBot.Core.Abstractions.Services;
using UBot.Core.Network;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreSpawnRuntime : ISpawnRuntime
{
    public object Player => Game.Player;

    public object SelectedEntity
    {
        get => Game.SelectedEntity;
        set => Game.SelectedEntity = value as SpawnedBionic;
    }

    public object GetRefObjCommon(uint id) => Game.ReferenceManager.GetRefObjCommon(id);

    public SpawnParseResult ParseSpawn(object packet, bool isGroup)
    {
        if (packet is not Packet networkPacket)
            return null;

        var refObjId = networkPacket.ReadUInt();

        if (refObjId == uint.MaxValue)
            return new SpawnParseResult(SpawnedSpellArea.FromPacket(networkPacket), null);

        if (refObjId == 0xfffffffe)
        {
            networkPacket.ReadUInt();
            networkPacket.ReadUInt();
            return null;
        }

        var obj = Game.ReferenceManager.GetRefObjCommon(refObjId);
        if (obj == null)
        {
            Log.Debug($"SpawnManager::Parse error while getting RefObjCommon by id {refObjId}");
            return null;
        }

        SpawnedEntity entity = null;
        string eventName = null;

        switch (obj.TypeID1)
        {
            case 1:
                switch (obj.TypeID2)
                {
                    case 1:
                        var spawnedPlayer = new SpawnedPlayer(refObjId);
                        spawnedPlayer.Deserialize(networkPacket);
                        entity = spawnedPlayer;
                        eventName = "OnSpawnPlayer";
                        break;

                    case 2:
                        switch (obj.TypeID3)
                        {
                            case 1:
                                var spawnedMonster = new SpawnedMonster(refObjId);
                                spawnedMonster.Deserialize(networkPacket);
                                entity = spawnedMonster;
                                eventName = "OnSpawnMonster";
                                break;

                            case 3:
                                var spawnedCos = new SpawnedCos(refObjId);
                                spawnedCos.Deserialize(networkPacket);
                                entity = spawnedCos;
                                eventName = "OnSpawnCos";
                                break;

                            case 5:
                                var spawnedFortressStructure = new SpawnedFortressStructure(refObjId);
                                spawnedFortressStructure.Deserialize(networkPacket);
                                entity = spawnedFortressStructure;
                                eventName = "OnSpawnFortressStructure";
                                break;

                            default:
                                var spawnedNpc = new SpawnedNpcNpc(refObjId);
                                spawnedNpc.ParseBionicDetails(networkPacket);
                                spawnedNpc.Deserialize(networkPacket);
                                entity = spawnedNpc;
                                eventName = "OnSpawnNpc";
                                break;
                        }
                        break;
                }
                break;

            case 3:
                entity = SpawnedItem.FromPacket(networkPacket, refObjId);
                eventName = "OnSpawnItem";
                break;

            case 4:
                entity = SpawnedPortal.FromPacket(networkPacket, refObjId);
                eventName = "OnSpawnPortal";
                break;
        }

        if (!isGroup && entity != null)
        {
            if (obj.TypeID1 == 1 || obj.TypeID1 == 4)
            {
                networkPacket.ReadByte();
            }
            else if (obj.TypeID1 == 3)
            {
                networkPacket.ReadByte();
                networkPacket.ReadUInt();
            }
        }

        return entity == null ? null : new SpawnParseResult(entity, eventName);
    }

    public void FireEvent(string eventName, params object[] args)
    {
        Event.EventManager.FireEvent(eventName, args);
    }

    public void LogDebug(string message)
    {
        Log.Debug(message);
    }
}
