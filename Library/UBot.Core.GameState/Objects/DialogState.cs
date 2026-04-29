using System;
using UBot.Core.Abstractions;

namespace UBot.Core.Objects;

public class DialogState
{
    private uint _dialogNpcId;
    public uint RequestedCloseNpcId = 0;

    public uint RequestedNpcId = 0;
    public bool IsInDialog => _dialogNpcId != 0;
    public TalkOption TalkOption { get; set; }

    public bool IsSpecialityTime { get; set; }

    public dynamic Npc
    {
        get =>
            _dialogNpcId == 0
                ? null
                : GameStateRuntimeProvider.Instance?.GetEntity(
                    Type.GetType("UBot.Core.Objects.Spawn.SpawnedNpc, UBot.Core"),
                    _dialogNpcId
                );
        set => _dialogNpcId = value?.UniqueId ?? 0;
    }
}
