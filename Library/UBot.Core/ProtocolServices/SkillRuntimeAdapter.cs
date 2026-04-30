using System.Collections.Generic;
using UBot.Core.Abstractions.Services;
using UBot.Core.Network;

namespace UBot.Core.ProtocolServices;

internal sealed class SkillRuntimeAdapter : ISkillRuntime
{
    public void SendToServer(object packet, params object[] callbacks)
    {
        if (packet is not Packet networkPacket)
            return;

        var typedCallbacks = new List<AwaitCallback>();
        if (callbacks != null)
        {
            foreach (var callback in callbacks)
                if (callback is AwaitCallback typedCallback)
                    typedCallbacks.Add(typedCallback);
        }

        UBot.Core.RuntimeAccess.Packets.SendPacket(networkPacket, PacketDestination.Server, typedCallbacks.ToArray());
    }

    public object CreateActionAckCallback()
    {
        return new AwaitCallback(
            response =>
                response.ReadByte() == 0x02 && response.ReadByte() == 0x00
                    ? AwaitCallbackResult.Success
                    : AwaitCallbackResult.ConditionFailed,
            0xB074
        );
    }

    public object CreateBuffCastCallback(uint targetId, uint skillId)
    {
        return new AwaitCallback(
            response =>
            {
                var responseTargetId = response.ReadUInt();
                var castedSkillId = response.ReadUInt();

                return responseTargetId == targetId && castedSkillId == skillId
                    ? AwaitCallbackResult.Success
                    : AwaitCallbackResult.ConditionFailed;
            },
            0xB0BD
        );
    }

    public void AwaitCallback(object callback, int timeoutMilliseconds = 0)
    {
        if (callback is AwaitCallback typedCallback)
            typedCallback.AwaitResponse(timeoutMilliseconds == 0 ? 5000 : timeoutMilliseconds);
    }

    public bool IsCallbackCompleted(object callback)
    {
        return callback is AwaitCallback typedCallback && typedCallback.IsCompleted;
    }
}
