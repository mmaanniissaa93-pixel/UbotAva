using UBot.Core;
using UBot.Core.Cryptography;
using UBot.Core.Network;
using UBot.General.Components;

namespace UBot.General.PacketHandler;

internal class AgentLoginRequestHook : IPacketHook
{
    /// <summary>
    ///     Gets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x6103;

    /// <summary>
    ///     Gets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Server;

    /// <summary>
    ///     Replaces the packet and returns a new packet.
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
    public Packet ReplacePacket(Packet packet)
    {
        if (!AutoLogin.ConsumeAgentCredentialRewrite())
            return packet;

        var username = UBot.Core.RuntimeAccess.Global.Get<string>("UBot.General.AutoLoginAccountUsername");

        var selectedAccount = Accounts.SavedAccounts.Find(p => p.Username == username);
        if (selectedAccount == null)
            return packet;

        if (UBot.Core.RuntimeAccess.Session.Clientless)
            return packet;

        // Preserve optional anti-cheat payload bytes (e.g. MaxiGuard variants)
        // from the original client packet when present.
        var locale = UBot.Core.RuntimeAccess.Session.ReferenceManager.DivisionInfo.Locale;
        var macAddress = UBot.Core.RuntimeAccess.Session.MacAddress;
        byte[] tailBytes = System.Array.Empty<byte>();

        try
        {
            var original = new Packet(packet);
            if (!original.Locked)
                original.Lock();

            if (original.Remaining >= 4)
                original.ReadUInt(); // token

            if (original.Remaining >= 2)
                original.ReadString(); // username

            if (original.Remaining >= 2)
                original.ReadString(); // password

            if (original.Remaining >= 1)
                locale = original.ReadByte();

            if (original.Remaining >= 6)
                macAddress = original.ReadBytes(6);

            if (original.Remaining > 0)
                tailBytes = original.ReadBytes(original.Remaining);
        }
        catch
        {
            // Ignore parse failures; fallback to legacy behavior below.
        }

        if (macAddress == null || macAddress.Length != 6)
            macAddress = UBot.Core.RuntimeAccess.Session.MacAddress;
        if (macAddress == null || macAddress.Length != 6)
            macAddress = new byte[6];

        packet = new Packet(packet.Opcode, packet.Encrypted);
        packet.WriteUInt(UBot.Core.RuntimeAccess.Core.Proxy.Token);

        if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.RuSro)
        {
            packet.WriteString(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.RuSro.login"));
            packet.WriteString(Sha256.ComputeHash(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.RuSro.password")));
        }
        else if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Japanese)
        {
            packet.WriteString(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.JSRO.Login"));
            packet.WriteString(Sha256.ComputeHash(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.JSRO.Token")));
        }
        else
        {
            if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Global && selectedAccount.Channel == 0x02)
                packet.WriteString(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.JCPlanet.Login"));
            else
                packet.WriteString(selectedAccount.Username);

            if (AutoLogin.ShouldHashPasswordForCurrentClient())
                packet.WriteString(Sha256.ComputeHash(selectedAccount.Password));
            else
                packet.WriteString(selectedAccount.Password);
        }

        packet.WriteByte(locale);
        packet.WriteBytes(macAddress);
        if (tailBytes.Length > 0)
            packet.WriteBytes(tailBytes);
        packet.Lock();

        return packet;
    }
}
