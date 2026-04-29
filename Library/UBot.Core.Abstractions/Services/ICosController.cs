namespace UBot.Core.Abstractions.Services;

public interface ICosController
{
    object AbilityPet { get; }
    object Fellow { get; }
    object Growth { get; }
    object JobTransport { get; }
    object Transport { get; }
    object Vehicle { get; set; }
}
