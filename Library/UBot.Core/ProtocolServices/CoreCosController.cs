using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreCosController : ICosController
{
    public object AbilityPet => UBot.Core.RuntimeAccess.Session.Player?.AbilityPet;

    public object Fellow => UBot.Core.RuntimeAccess.Session.Player?.Fellow;

    public object Growth => UBot.Core.RuntimeAccess.Session.Player?.Growth;

    public object JobTransport => UBot.Core.RuntimeAccess.Session.Player?.JobTransport;

    public object Transport => UBot.Core.RuntimeAccess.Session.Player?.Transport;

    public object Vehicle
    {
        get => UBot.Core.RuntimeAccess.Session.Player?.Vehicle;
        set
        {
            if (UBot.Core.RuntimeAccess.Session.Player != null)
                UBot.Core.RuntimeAccess.Session.Player.Vehicle = value;
        }
    }
}

