using System.Linq;
using UBot.Core;
using UBot.Core.Event;
using UBot.Party.Bundle;

namespace UBot.Party.Subscribers;

internal class PartySubscriber
{
    /// <summary>
    ///     Gets the subscribed events.
    /// </summary>
    /// <returns></returns>
    public static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnPartyRequest", OnPartyRequest);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnLoadCharacter", Container.Refresh);
    }

    /// <summary>
    ///     Checks the request.
    /// </summary>
    /// <returns></returns>
    private static bool CheckRequest()
    {
        //Check for the pending request
        if (!UBot.Core.RuntimeAccess.Session.Party.HasPendingRequest)
            return false;

        Log.NotifyLang("PartyPlayerInvite", UBot.Core.RuntimeAccess.Session.AcceptanceRequest.Player.Name);

        //Check if we are already in a party - don't auto-accept if we're already in a party
        if (UBot.Core.RuntimeAccess.Session.Party.IsInParty)
            return false;

        //Check if we are near the training place
        if (
            Container.AutoParty.Config.OnlyAtTrainingPlace
            && UBot.Core.RuntimeAccess.Session.Player.Movement.Source.DistanceTo(Container.AutoParty.Config.CenterPosition) > 50
        )
            return false;

        //Check if the inviting player matches our party list
        if (
            Container.AutoParty.Config.AcceptFromList
            && Container.AutoParty.Config.PlayerList.Contains(UBot.Core.RuntimeAccess.Session.AcceptanceRequest.Player.Name)
        )
            return true;

        //Accept all invitations if enabled
        if (Container.AutoParty.Config.AcceptAll)
            return true;

        return false;
    }

    /// <summary>
    ///     Will be fired when the player is being invited to a party
    /// </summary>
    private static void OnPartyRequest()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running && !Container.AutoParty.Config.AcceptIfBotIsStopped)
            return;

        if (CheckRequest())
            UBot.Core.RuntimeAccess.Session.AcceptanceRequest.Accept();
        else
            UBot.Core.RuntimeAccess.Session.AcceptanceRequest.Refuse();
    }
}
