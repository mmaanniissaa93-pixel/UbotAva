using UBot.Core.Client.ReferenceObjects;

namespace UBot.Core.Abstractions.Services;

public sealed class AlchemyProgressUpdate
{
    public AlchemyProgressUpdate(AlchemyOperationState state, int percent, string message, AlchemyType? type = null)
    {
        State = state;
        Percent = percent;
        Message = message;
        Type = type;
    }

    public AlchemyOperationState State { get; }
    public int Percent { get; }
    public string Message { get; }
    public AlchemyType? Type { get; }
}
