using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface IAlchemyService
{
    IReadOnlyList<object> ActiveAlchemyItems { get; }
    bool IsFusing { get; }

    void CancelPending();
    bool TryFuseElixir(object item, object elixir, object powder);
    bool TryFuseMagicStone(object item, object magicStone);
    bool TryFuseAttributeStone(object item, object attributeStone);
}
