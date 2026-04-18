using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Network;

namespace UBot.Party.Bundle.PartyMatching.Network;

public class PartyMatchingDeleteResponse : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>The destination.</value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>The opcode.</value>
    public ushort Opcode => 0xB06B;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        if (packet.ReadByte() != 0x01)
            return;

        Log.NotifyLang("PartyEntryRemoved");

        if (Container.PartyMatching != null)
        {
            Container.PartyMatching.CancelScheduledDeletion();
            Container.PartyMatching.HasMatchingEntry = false;

            if (Container.PartyMatching.Config.AutoReform)
            {
                _ = Task.Run(async () =>
                {
                    // Server can reject immediate recreate right after delete; retry briefly.
                    await Task.Delay(350).ConfigureAwait(false);
                    for (var attempt = 0; attempt < 3; attempt++)
                    {
                        if (Container.PartyMatching.HasMatchingEntry)
                            return;

                        if (Container.PartyMatching.Create())
                            return;

                        await Task.Delay(900).ConfigureAwait(false);
                    }
                });
            }
        }

        EventManager.FireEvent("OnDeletePartyEntry");
    }
}