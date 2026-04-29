using System;
using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface ISpawnService
{
    object GetEntity(Type entityType, uint uniqueId);
    object FindEntity(Type entityType, Func<object, bool> predicate);
    bool TryGetEntityIncludingMe(uint uniqueId, out object entity);
    bool TryGetEntities(Type entityType, Func<object, bool> predicate, out IEnumerable<object> entities);
    void Parse(object packet, bool isGroup = false);
    void Update(int delta);
    void Clear();
}
