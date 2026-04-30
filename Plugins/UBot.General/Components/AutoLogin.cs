#nullable enable annotations

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Cryptography;
using UBot.Core.Event;
using UBot.Core.Network;
using UBot.Core.Network.Protocol;
using UBot.General.Models;
using Server = UBot.General.Models.Server;

namespace UBot.General.Components;

internal static class AutoLogin
{
    /// <summary>
    ///     Is the auto login pending <c>true</c> otherwise; <c>false</c>
    /// </summary>
    public static bool Pending;

    public static CancellationTokenSource? Cts { get; private set; }

    /// <summary>
    ///     Is the auto login handling <c>true</c> otherwise; <c>false</c>
    /// </summary>
    private static bool _busy;
    private static int _agentCredentialRewriteArmed;
    private static bool _rigidUseHashedPassword = true;
    private static bool _rigidCredentialRetryUsed;

    internal static bool IsHandling => _busy;
    internal static bool IsAgentCredentialRewriteArmed => Volatile.Read(ref _agentCredentialRewriteArmed) == 1;

    /// <summary>
    ///     Does the automatic login.
    /// </summary>
    public static async void Handle()
    {
        if (Pending)
            return;

        if (_busy)
            return;

        Log.StatusLang("WaitingUser");

        _busy = true;
        try
        {
            if (!UBot.Core.RuntimeAccess.Global.Get<bool>("UBot.General.EnableAutomatedLogin"))
                return;

            var selectedAccount = Accounts.SavedAccounts?.Find(p =>
                p.Username == UBot.Core.RuntimeAccess.Global.Get<string>("UBot.General.AutoLoginAccountUsername")
            );
            if (selectedAccount == null)
            {
                Log.WarnLang("NoHaveAccountForAutoLogin");
                await Task.Delay(5000);
                ClientlessManager.RequestServerList();
                return;
            }

            var server = Serverlist.GetServerByName(selectedAccount.ServerName);
            if (server == null)
            {
                Log.NotifyLang("ServerNotFound", selectedAccount.ServerName);

                if (Serverlist.Servers.Count == 0)
                {
                    Log.NotifyLang("ServerCheck");
                    await Task.Delay(5000);
                    ClientlessManager.RequestServerList();
                    return;
                }

                server = Serverlist.Servers.First();
                Log.NotifyLang("SelectedFirstServer", server.Name);
            }

            // is server check [Lazy :)]
            if (!server.Status)
            {
                Log.NotifyLang("ServerCheck");
                await Task.Delay(5000);
                ClientlessManager.RequestServerList();
                return;
            }

            //Wait for the configured delay before sending the login request
            //It is possible to cancel in case of manual login to the server
            if (UBot.Core.RuntimeAccess.Global.Get("UBot.General.EnableLoginDelay", false))
            {
                var delay = UBot.Core.RuntimeAccess.Global.Get("UBot.General.LoginDelay", 10) * 1000;
                Cts = new CancellationTokenSource();

                try
                {
                    await Task.Delay(delay, Cts.Token);
                }
                catch (TaskCanceledException)
                {
                    Log.Debug("Manual login has been detected. AutoLogin is cancelled this time!");
                    return;
                }
                finally
                {
                    Cts.Dispose();
                    Cts = null;
                }
            }

            SendLoginRequest(selectedAccount, server);
        }
        catch (Exception ex)
        {
            Log.Error($"AutoLogin failed: {ex.Message}");
        }
        finally
        {
            _busy = false;
        }
    }

    internal static bool ShouldHashPasswordForCurrentClient()
    {
        if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Rigid)
            return _rigidUseHashedPassword;

        return UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Turkey
            || UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.VTC_Game
            || UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Global
            || UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Korean
            || UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Taiwan;
    }

    internal static void MarkRigidAuthSuccess()
    {
        if (UBot.Core.RuntimeAccess.Session.ClientType != GameClientType.Rigid)
            return;

        _rigidCredentialRetryUsed = false;
    }

    internal static bool TryScheduleRigidCredentialRetry(string stage)
    {
        if (UBot.Core.RuntimeAccess.Session.ClientType != GameClientType.Rigid)
            return false;

        if (!UBot.Core.RuntimeAccess.Global.Get("UBot.General.EnableAutomatedLogin", false))
            return false;

        if (_rigidCredentialRetryUsed)
            return false;

        _rigidCredentialRetryUsed = true;
        _rigidUseHashedPassword = !_rigidUseHashedPassword;

        var mode = _rigidUseHashedPassword ? "SHA256" : "PLAIN";
        Log.Warn($"Rigid auto-login retry ({stage}): switching password mode to {mode}.");
        Task.Delay(1000).ContinueWith(_ => Handle());
        return true;
    }

    internal static void ArmAgentCredentialRewrite()
    {
        Interlocked.Exchange(ref _agentCredentialRewriteArmed, 1);
    }

    internal static void SetAgentCredentialRewrite(bool enabled)
    {
        Interlocked.Exchange(ref _agentCredentialRewriteArmed, enabled ? 1 : 0);
    }

    internal static bool ConsumeAgentCredentialRewrite()
    {
        return Interlocked.Exchange(ref _agentCredentialRewriteArmed, 0) == 1;
    }

    /// <summary>
    ///     Sends the secondary password if have.
    /// </summary>
    internal static void SendSecondaryPassword()
    {
        if (Accounts.Joined == null)
            return;

        if (!UBot.Core.RuntimeAccess.Global.Get<bool>("UBot.General.EnableAutomatedLogin"))
            return;

        var secondaryPassword = Accounts.Joined.SecondaryPassword;

        if (string.IsNullOrWhiteSpace(secondaryPassword))
            return;

        Blowfish blowfish = new();
        byte[] key = { 0x0F, 0x07, 0x3D, 0x20, 0x56, 0x62, 0xC9, 0xEB };

        if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Rigid)
            key = key.Reverse().ToArray();

        blowfish.Initialize(key);

        var encodedBuffer = blowfish.Encode(Encoding.ASCII.GetBytes(secondaryPassword));

        var packet = new Packet(0x6117, true);
        packet.WriteByte(4);
        packet.WriteUShort(secondaryPassword.Length);
        packet.WriteBytes(encodedBuffer);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    /// <summary>
    ///     Sends the login request.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <param name="server">The server.</param>
    private static void SendLoginRequest(Account account, Server server)
    {
        Log.NotifyLang("LoginCredentials", server.Name);

        ushort opcode = 0x6102;
        if (UBot.Core.RuntimeAccess.Session.ClientType >= GameClientType.Chinese)
            opcode = 0x610A;

        var loginPacket = new Packet(opcode, true);
        loginPacket.WriteByte(UBot.Core.RuntimeAccess.Session.ReferenceManager.DivisionInfo.Locale);
        if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.RuSro)
        {
            loginPacket.WriteString(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.RuSro.login"));
            loginPacket.WriteString(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.RuSro.password"));
        }
        else if (UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Japanese)
        {
            loginPacket.WriteString(string.Empty);
            loginPacket.WriteString(UBot.Core.RuntimeAccess.Global.Get<string>("UBot.JSRO.Token"));
        }
        else
        {
            loginPacket.WriteString(account.Username);
            loginPacket.WriteString(
                ShouldHashPasswordForCurrentClient()
                    ? Sha256.ComputeHash(account.Password)
                    : account.Password
            );
        }

        UBot.Core.RuntimeAccess.Session.MacAddress = GenerateMacAddress();

        if (
            UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Turkey
            || UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.VTC_Game
            || UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.RuSro
            || UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Korean
            || UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Japanese
            || UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Taiwan
        )
            loginPacket.WriteBytes(UBot.Core.RuntimeAccess.Session.MacAddress);

        loginPacket.WriteUShort(server.Id);

        if (opcode == 0x610A)
            loginPacket.WriteByte(account.Channel);

        ArmAgentCredentialRewrite();
        UBot.Core.RuntimeAccess.Packets.SendPacket(loginPacket, PacketDestination.Server);

        Accounts.Joined = account;
        Serverlist.Joining = server;
    }

    /// <summary>
    ///     Generates valid MAC address.
    /// </summary>
    /// <returns></returns>
    private static byte[] GenerateMacAddress()
    {
        Random rand = new Random();
        byte firstByte = (byte)(rand.Next(0, 256) & 0xFE);

        byte[] macBytes = new byte[6];
        macBytes[0] = firstByte;
        for (int i = 1; i < 6; i++)
        {
            macBytes[i] = (byte)rand.Next(0, 256);
        }

        return macBytes;
    }

    /// <summary>
    ///     Sends the static captcha.
    /// </summary>
    public static void SendStaticCaptcha()
    {
        if (
            !UBot.Core.RuntimeAccess.Global.Get<bool>("UBot.General.EnableStaticCaptcha")
            || !UBot.Core.RuntimeAccess.Global.Get<bool>("UBot.General.EnableAutomatedLogin")
        )
            return;

        var captcha = UBot.Core.RuntimeAccess.Global.Get<string>("UBot.General.StaticCaptcha");
        captcha ??= string.Empty;

        Log.NotifyLang("EnteringCaptcha", captcha);

        var packet = new Packet(0x6323);
        packet.WriteString(captcha);

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    /// <summary>
    ///     Enters the game.
    /// </summary>
    /// <param name="character">The character.</param>
    public static void EnterGame(string character)
    {
        if (!UBot.Core.RuntimeAccess.Global.Get<bool>("UBot.General.EnableAutomatedLogin"))
            return;

        var packet = new Packet(0x7001);
        packet.WriteString(character);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);

        ProfileManager.SelectedCharacter = character;
        UBot.Core.RuntimeAccess.Player.Load(character);

        UBot.Core.RuntimeAccess.Events.FireEvent("OnEnterGame");
    }
}
