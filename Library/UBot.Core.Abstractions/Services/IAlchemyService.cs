using System.Collections.Generic;
using UBot.Core.Client.ReferenceObjects;

namespace UBot.Core.Abstractions.Services;

public interface IAlchemyService
{
    IReadOnlyList<object> ActiveAlchemyItems { get; }
    bool IsFusing { get; }
    AlchemyOperationState State { get; }

    void CancelPending();
    bool TryFuseElixir(object item, object elixir, object powder = null);
    bool TryFuseMagicStone(object item, object magicStone);
    bool TryFuseAttributeStone(object item, object attributeStone);
    void BeginFuseRequest(IReadOnlyList<object> items, AlchemyAction action, AlchemyType type);
    void MarkCanceled(AlchemyType type);
    void MarkDestroyed(object oldItem, AlchemyType type);
    void MarkError(ushort errorCode, AlchemyType type);
    void MarkResult(bool success, object oldItem, object newItem, AlchemyType type);
    void ClearActiveItems();
}
