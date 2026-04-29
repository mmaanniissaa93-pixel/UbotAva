namespace UBot.Core.Abstractions.Services;

public enum AlchemyOperationState
{
    Idle,
    Validating,
    PacketPrepared,
    WaitingForAck,
    Succeeded,
    Failed,
    Canceled,
    Destroyed,
    Error,
    TimedOut
}
