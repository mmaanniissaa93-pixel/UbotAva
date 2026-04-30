using UBot.Core;
using UBot.Core.Cryptography;
using UBot.Core.Network;
using UBot.General.Components;

namespace UBot.General.PacketHandler;

internal class GlobalIdentificationRequest : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x2001;

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
        if (!UBot.Core.RuntimeAccess.Session.Clientless)
            return;

        var serviceName = packet.ReadString();

        if (serviceName == "GatewayServer")
        {
            var response = new Packet(0x6100);
            response.WriteByte(UBot.Core.RuntimeAccess.Session.ReferenceManager.DivisionInfo.Locale);
            response.WriteString("SR_Client");
            response.WriteUInt(UBot.Core.RuntimeAccess.Session.ReferenceManager.VersionInfo.Version);

            UBot.Core.RuntimeAccess.Packets.SendPacket(response, PacketDestination.Server);
        }
        else if (serviceName == "AgentServer")
        {
            var selectedAccount = Accounts.SavedAccounts.Find(p =>
                p.Username == UBot.Core.RuntimeAccess.Global.Get<string>("UBot.General.AutoLoginAccountUsername")
            );
            if (selectedAccount == null)
            {
                Log.WarnLang("UBot.General", "AgentServerConnectingError");
                return;
            }

            Log.NotifyLang("UBot.General", "AuthAgentCertify");

            var opcode = (ushort)(UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Rigid ? 0x6118 : 0x6103);
            var response = new Packet(opcode, true);
            response.WriteUInt(UBot.Core.RuntimeAccess.Core.Proxy.Token);

            if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.RuSro)
            {
                response.WriteString(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.RuSro.login"));
                response.WriteString(Sha256.ComputeHash(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.RuSro.password")));
            }
            else if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Japanese)
            {
                response.WriteString(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.JSRO.Login"));
                response.WriteString(Sha256.ComputeHash(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.JSRO.Token")));
            }
            else
            {
                if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Global && selectedAccount.Channel == 0x02)
                    response.WriteString(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.JCPlanet.Login"));
                else
                    response.WriteString(selectedAccount.Username);

                if (AutoLogin.ShouldHashPasswordForCurrentClient())
                    response.WriteString(Sha256.ComputeHash(selectedAccount.Password));
                else
                    response.WriteString(selectedAccount.Password);
            }

            response.WriteByte(UBot.Core.RuntimeAccess.Session.ReferenceManager.DivisionInfo.Locale);
            response.WriteBytes(UBot.Core.RuntimeAccess.Session.MacAddress);
            UBot.Core.RuntimeAccess.Packets.SendPacket(response, PacketDestination.Server);
        }
    }
}
