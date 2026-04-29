using System;
using System.Collections.Generic;
using System.Linq;
using UBot.Core.Abstractions.Services;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using UBot.Core.Services;

namespace UBot.Core.Components;

public static class SpawnManager
{
    private static readonly object _lock = new();
    private static List<SpawnedEntity> _entities = new(512);
    private static readonly Dictionary<uint, SpawnedEntity> _entityIndex = new();

    public static T GetEntity<T>(uint uniqueId)
        where T : SpawnedEntity
    {
        return _entityIndex.TryGetValue(uniqueId, out var entity) ? entity as T : null;
    }

    public static T GetEntity<T>(Func<T, bool> condition)
        where T : SpawnedEntity
    {
        return (T)_entities.Find(p => p is T entityT && condition(entityT));
    }

    public static bool TryGetEntity<T>(uint uniqueId, out T entity)
        where T : SpawnedEntity
    {
        entity = GetEntity<T>(uniqueId);
        return entity != null;
    }

    public static bool TryGetEntityIncludingMe(uint uniqueId, out SpawnedEntity entity)
    {
        entity = null;
        var player = Runtime?.Player as Player;

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

    public static bool TryGetEntity<T>(Func<T, bool> condition, out T entity)
        where T : SpawnedEntity
    {
        entity = GetEntity(condition);
        return entity != null;
    }

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

    public static int Count<T>(Func<T, bool> predicate)
        where T : SpawnedEntity
    {
        lock (_lock)
        {
            return _entities.Count(p => p is T && predicate(p as T));
        }
    }

    public static bool Any<T>(Func<T, bool> predicate)
        where T : SpawnedEntity
    {
        lock (_lock)
        {
            return _entities.Any(p => p is T && predicate(p as T));
        }
    }

    public static bool TryRemove(uint uniqueId, out SpawnedEntity removedEntity)
    {
        lock (_lock)
        {
            if (!_entityIndex.TryGetValue(uniqueId, out removedEntity))
                return false;

            if (Runtime?.SelectedEntity is SpawnedEntity selected && selected.UniqueId == uniqueId)
                Runtime.SelectedEntity = null;

            removedEntity.Dispose();
            _entityIndex.Remove(uniqueId);
            _entities.Remove(removedEntity);
            return true;
        }
    }

    public static int Clear<T>()
    {
        lock (_lock)
        {
            return _entities.RemoveAll(p => p is T && p.Dispose());
        }
    }

    public static void Parse(Packet packet, bool isGroup = false)
    {
        var result = Runtime?.ParseSpawn(packet, isGroup);
        if (result?.Entity is not SpawnedEntity entity)
            return;

        lock (_lock)
        {
            _entities.Add(entity);
            AddToIndex(entity);
            if (result.EventName != null)
                Runtime?.FireEvent(result.EventName, entity);
        }
    }

    public static void Update(int delta)
    {
        lock (_lock)
        {
            foreach (var entity in _entities)
                entity.Update(delta);
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _entities = new List<SpawnedEntity>(255);
            _entityIndex.Clear();
        }
    }

    private static void AddToIndex(SpawnedEntity entity)
    {
        if (entity == null)
            return;

        var uniqueId = entity.UniqueId;

        if (_entityIndex.TryGetValue(uniqueId, out var existingEntity))
        {
            _entities.Remove(existingEntity);
            ServiceRuntime.Log?.Debug($"SpawnManager: Duplicate uniqueId {uniqueId} replaced. Old entity removed from list.");
        }

        _entityIndex[uniqueId] = entity;
    }

    private static ISpawnRuntime Runtime => ServiceRuntime.SpawnRuntime;
}
