namespace UBot.Core.Abstractions.Services;

public interface IAlchemyProgress
{
    void Report(AlchemyProgressUpdate update);
}
