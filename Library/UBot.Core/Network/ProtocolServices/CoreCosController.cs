using UBot.Core.Abstractions.Services;

namespace UBot.Core.Network.ProtocolServices;

internal sealed class CoreCosController : ICosController
{
    public object AbilityPet => Game.Player?.AbilityPet;

    public object Fellow => Game.Player?.Fellow;

    public object Growth => Game.Player?.Growth;

    public object JobTransport => Game.Player?.JobTransport;

    public object Transport => Game.Player?.Transport;

    public object Vehicle
    {
        get => Game.Player?.Vehicle;
        set
        {
            if (Game.Player != null)
                Game.Player.Vehicle = value;
        }
    }
}
