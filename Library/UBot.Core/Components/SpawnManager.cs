using System;
using System.Collections.Generic;
using System.Linq;
using UBot.Core.Event;
using UBot.Core.Network;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.Components;

public static class SpawnManager
{
    /// <summary>
    ///     The locking object
    /// </summary>
    private static readonly object _lock = new();

    /// <summary>
    ///     The game spawned entities on the area
    /// </summary>
    private static List<SpawnedEntity> _entities = new(255);

    /// <summary>
    ///     O(1) lookup index by UniqueId
    /// </summary>
    private static readonly Dictionary<uint, SpawnedEntity> _entityIndex = new();

    /// <summary>
    ///     Get entity by unique id with specified generic type.
    /// </summary>
    /// <param name="uniqueId">The unique identifier.</param>
    /// <returns></returns>
    public static T GetEntity<T>(uint uniqueId)
        where T : SpawnedEntity
    {
        return _entityIndex.TryGetValue(uniqueId, out var entity) ? entity as T : null;
    }

    /// <summary>
    ///     Get entity by unique id with specified generic type.
    /// </summary>
    /// <param name="uniqueId">The unique identifier.</param>
    /// <returns><c>true</c> is succesfully found; otherwise <c>false</c></returns>
    public static T GetEntity<T>(Func<T, bool> condition)
        where T : SpawnedEntity
    {
        return (T)_entities.Find(p => p is T entityT && condition(entityT));
    }

    /// <summary>
    ///     Try get an entity by the specified unique identifier.
    /// </summary>
    /// <param name="uniqueId">The searching uniqueId of the entity</param>
    /// <param name="removedEntity">Returning founded entity</param>
    /// <returns><c>true</c> if success; otherwise <c>false</c></returns>
    public static bool TryGetEntity<T>(uint uniqueId, out T entity)
        where T : SpawnedEntity
    {
        entity = GetEntity<T>(uniqueId);
        return entity != null;
    }

    /// <summary>
    ///     Try get an entity by the specified unique identifier.
    /// </summary>
    /// <param name="uniqueId">The searching uniqueId of the entity</param>
    /// <param name="removedEntity">Returning founded entity</param>
    /// <returns><c>true</c> if success; otherwise <c>false</c></returns>
    public static bool TryGetEntityIncludingMe(uint uniqueId, out SpawnedEntity entity)
    {
        entity = null;
        var player = Game.Player;

        if (player != null && uniqueId == player.UniqueId)
            entity = player;
        else if (player?.Transport?.UniqueId == uniqueId)
            entity = player.Transport;
        else if (player?.JobTransport?.UniqueId == uniqueId)
            entity = player.JobTransport;
        else if (player?.Growth?.UniqueId == uniqueId)
            entity = player.Growth;
        else if (player?.Fellow?.UniqueId == uniqueId)
            entity = player.Fellow;
        else if (!TryGetEntity(uniqueId, out entity))
            return false;

        return entity != null;
    }

    /// <summary>
    ///     Try get entity by unique id with specified generic type.
    /// </summary>
    /// <param name="uniqueId">The unique identifier.</param>
    /// <returns><c>true</c> is succesfully found; otherwise <c>false</c></returns>
    public static bool TryGetEntity<T>(Func<T, bool> condition, out T entity)
        where T : SpawnedEntity
    {
        entity = GetEntity<T>(p => condition(p));
        return entity != null;
    }

    /// <summary>
    ///     Try get entities by conditions with specified generic type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entities">The entities</param>
    /// <param name="predicate">The condition</param>
    /// <returns><c>true</c> if successfully found; otherwise <c>false</c></returns>
    public static bool TryGetEntities<T>(out IEnumerable<T> entities)
        where T : SpawnedEntity
    {
        lock (_lock)
        {
            var result = new List<T>();
            var count = _entities.Count;
            for (var i = 0; i < count; i++)
            {
                if (_entities[i] is T entity)
                    result.Add(entity);
            }

            entities = result;
            return result.Count > 0;
        }
    }

    /// <summary>
    ///     Try get entities by conditions with specified generic type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entities">The entities</param>
    /// <param name="predicate">The condition</param>
    /// <returns><c>true</c> if successfully found; otherwise <c>false</c></returns>
    public static bool TryGetEntities<T>(Func<T, bool> predicate, out IEnumerable<T> entities)
        where T : SpawnedEntity
    {
        lock (_lock)
        {
            var result = new List<T>();
            var count = _entities.Count;
            for (var i = 0; i < count; i++)
            {
                if (_entities[i] is T entity && predicate(entity))
                    result.Add(entity);
            }

            entities = result;
            return result.Count > 0;
        }
    }

    /// <summary>
    ///     Try get entities by conditions with specified generic type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entities">The entities</param>
    /// <param name="predicate">The condition</param>
    /// <returns><c>true</c> if successfully found; otherwise <c>false</c></returns>
    public static int Count<T>(Func<T, bool> predicate)
        where T : SpawnedEntity
    {
        lock (_lock)
        {
            return _entities.Count(p => p is T && predicate(p as T));
        }
    }

    /// <summary>
    ///     Try get entities by conditions with specified generic type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entities">The entities</param>
    /// <param name="predicate">The condition</param>
    /// <returns><c>true</c> if successfully found; otherwise <c>false</c></returns>
    public static bool Any<T>(Func<T, bool> predicate)
        where T : SpawnedEntity
    {
        lock (_lock)
        {
            return _entities.Any(p => p is T && predicate(p as T));
        }
    }

    /// <summary>
    ///     Try remove an entity by the specified unique identifier.
    /// </summary>
    /// <param name="uniqueId">The removing uniqueId of the entity</param>
    /// <param name="removedEntity">Returning removed entity object</param>
    /// <returns><c>true</c> if success; otherwise <c>false</c></returns>
    public static bool TryRemove(uint uniqueId, out SpawnedEntity removedEntity)
    {
        lock (_lock)
        {
            if (!_entityIndex.TryGetValue(uniqueId, out removedEntity))
                return false;

            if (Game.SelectedEntity?.UniqueId == uniqueId)
                Game.SelectedEntity = null;

            removedEntity.Dispose();
            _entityIndex.Remove(uniqueId);
            _entities.Remove(removedEntity);
            return true;
        }
    }

    /// <summary>
    ///     Clears this instance.
    /// </summary>
    public static int Clear<T>()
    {
        lock (_lock)
        {
            return _entities.RemoveAll(p => p is T && p.Dispose());
        }
    }

    /// <summary>
    ///     Parse the incoming spawn packet
    /// </summary>
    /// <param name="packet">The packet</param>
    public static void Parse(Packet packet, bool isGroup = false)
    {
        lock (_lock)
        {
            var refObjId = packet.ReadUInt();

            if (refObjId == uint.MaxValue)
            {
                var spellArea = SpawnedSpellArea.FromPacket(packet);
                _entities.Add(spellArea);
                AddToIndex(spellArea);
                return;
            }

            // ghidra(isro client): FUN_009dd970 maybe flowers?
            if (refObjId == 0xfffffffe)
            {
                packet.ReadUInt();
                packet.ReadUInt();
            }

            var obj = Game.ReferenceManager.GetRefObjCommon(refObjId);
            if (obj == null)
            {
                Log.Debug($"SpawnManager::Parse error while getting RefObjCommon by id {refObjId}");

                return;
            }

            //Log.Debug($"Detected: {obj.GetRealName()}   {obj.CodeName}");

            switch (obj.TypeID1)
            {
                case 1:

                    switch (obj.TypeID2)
                    {
                        case 1:
                            {
                                var spawnedPlayer = new SpawnedPlayer(refObjId);
                                spawnedPlayer.Deserialize(packet);

                                _entities.Add(spawnedPlayer);
                                AddToIndex(spawnedPlayer);
                                EventManager.FireEvent("OnSpawnPlayer", spawnedPlayer);
                            }
                            break;

                        case 2:

                            switch (obj.TypeID3)
                            {
                                case 1:
                                    {
                                        var spawnedMonster = new SpawnedMonster(refObjId);
                                        spawnedMonster.Deserialize(packet);

                                        _entities.Add(spawnedMonster);
                                        AddToIndex(spawnedMonster);
                                        EventManager.FireEvent("OnSpawnMonster", spawnedMonster);
                                    }
                                    break;

                                case 3:
                                    {
                                        var spawnedCos = new SpawnedCos(refObjId);
                                        spawnedCos.Deserialize(packet);
                                        _entities.Add(spawnedCos);
                                        AddToIndex(spawnedCos);
                                        EventManager.FireEvent("OnSpawnCos", spawnedCos);
                                    }
                                    break;

                                case 5:
                                    {
                                        var spawnedFortressStructure = new SpawnedFortressStructure(refObjId);
                                        spawnedFortressStructure.Deserialize(packet);
                                        _entities.Add(spawnedFortressStructure);
                                        AddToIndex(spawnedFortressStructure);

                                        EventManager.FireEvent("OnSpawnFortressStructure", spawnedFortressStructure);
                                    }
                                    break;

                                default:
                                    {
                                        var spawnedNpc = new SpawnedNpcNpc(refObjId);
                                        spawnedNpc.ParseBionicDetails(packet);
                                        spawnedNpc.Deserialize(packet);
                                        _entities.Add(spawnedNpc);
                                        AddToIndex(spawnedNpc);
                                        EventManager.FireEvent("OnSpawnNpc", spawnedNpc);
                                    }
                                    break;
                            }

                            break;
                    }

                    break;

                case 3:
                    var spawnedItem = SpawnedItem.FromPacket(packet, refObjId);
                    _entities.Add(spawnedItem);
                    AddToIndex(spawnedItem);

                    EventManager.FireEvent("OnSpawnItem", spawnedItem);
                    break;

                case 4:
                    var spawnedPortal = SpawnedPortal.FromPacket(packet, refObjId);
                    _entities.Add(spawnedPortal);
                    AddToIndex(spawnedPortal);
                    EventManager.FireEvent("OnSpawnPortal", spawnedPortal);
                    break;
            }

            if (!isGroup)
            {
                if (obj.TypeID1 == 1 || obj.TypeID1 == 4)
                {
                    packet.ReadByte(); //1 = Normal, 3 = Spawning, 4 = Running
                }
                else if (obj.TypeID1 == 3)
                {
                    packet.ReadByte(); //DropSource
                    packet.ReadUInt(); //DropUID
                }
            }
        }
    }

    /// <summary>
    ///     Update the instance
    /// </summary>
    public static void Update(int delta)
    {
        lock (_lock)
        {
            foreach (var entity in _entities)
                entity.Update(delta);
        }
    }

    /// <summary>
    ///     Clear this instance.
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _entities = new List<SpawnedEntity>(255);
            _entityIndex.Clear();
        }
    }

    /// <summary>
    ///     Add entity to index
    /// </summary>
    private static void AddToIndex(SpawnedEntity entity)
    {
        if (entity != null)
            _entityIndex[entity.UniqueId] = entity;
    }

    /// <summary>
    ///     Remove entity from index
    /// </summary>
    private static void RemoveFromIndex(uint uniqueId)
    {
        _entityIndex.Remove(uniqueId);
    }
}
