using CoreGame = global::UBot.Core.Game;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using System;
using UBot.Core.Event;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Entity;

internal class EntityUpdateExperienceResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x3056;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        packet.ReadUInt(); //Mobs unique ID!

        long experienceAmount;

        if (CoreGame.ClientType >= GameClientType.Thailand)
            experienceAmount = packet.ReadLong();
        else
            experienceAmount = packet.ReadUInt();

        CoreGame.Player.Experience += experienceAmount;

        var iLevel = CoreGame.Player.Level;
        var oldLevel = CoreGame.Player.Level;

        while (CoreGame.Player.Experience > CoreGame.ReferenceManager.GetRefLevel(iLevel).Exp_C)
        {
            CoreGame.Player.Experience -= CoreGame.ReferenceManager.GetRefLevel(iLevel).Exp_C;
            iLevel++;
        }

        if (CoreGame.Player.Level < iLevel)
        {
            CoreGame.Player.StatPoints += Convert.ToUInt16((iLevel - oldLevel) * 3);
            CoreGame.Player.Level = iLevel;

            Log.Notify($"Congratulations, your level has increased to lv.{CoreGame.Player.Level}");

            EventManager.FireEvent("OnLevelUp", oldLevel);
        }

        EventManager.FireEvent("OnExpSpUpdate");
    }
}





