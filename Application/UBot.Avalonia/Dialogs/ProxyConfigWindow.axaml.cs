using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Net;
using UBot.Avalonia.Services;
using UBot.Core;

namespace UBot.Avalonia.Dialogs;

public partial class ProxyConfigWindow : Window
{
    public NetworkConfig? Config { get; private set; }
    public bool Applied { get; private set; }

    public ProxyConfigWindow()
    {
        InitializeComponent();
        ProxyTypeSelect.ItemsSource = new[] { "SOCKS5", "SOCKS4" };
        LoadFromGlobalConfig();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        var bindIp = (IpBindBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(bindIp))
            bindIp = "0.0.0.0";

        if (!IPAddress.TryParse(bindIp, out _))
            return;

        var active = ProxyActiveToggle.IsChecked == true;
        var proxyIp = (ProxyIpBox.Text ?? string.Empty).Trim();
        var proxyUser = (ProxyUserBox.Text ?? string.Empty).Trim();
        var proxyPass = ProxyPassBox.Text ?? string.Empty;
        var proxyType = ProxyTypeSelect.SelectedItem?.ToString()?.Trim().ToUpperInvariant() ?? "SOCKS5";
        var version = proxyType == "SOCKS4" ? 4 : 5;
        var proxyPort = ParseInt(ProxyPortBox.Text, 0);

        if (active)
        {
            if (string.IsNullOrWhiteSpace(proxyIp))
                return;
            if (proxyPort <= 0 || proxyPort > 65535)
                return;
        }

        UBot.Core.RuntimeAccess.Global.Set("UBot.Network.BindIp", bindIp);
        UBot.Core.RuntimeAccess.Global.SetArray(
            "UBot.Network.Proxy",
            new List<string>
            {
                active.ToString(),
                proxyIp,
                Math.Clamp(proxyPort, 0, 65535).ToString(CultureInfo.InvariantCulture),
                proxyUser,
                proxyPass,
                version.ToString(CultureInfo.InvariantCulture)
            },
            "|");
        UBot.Core.RuntimeAccess.Global.Save();

        Config = new NetworkConfig
        {
            BindIp = bindIp,
            Proxy = new ProxyConfig
            {
                Active = active,
                Ip = proxyIp,
                Port = Math.Clamp(proxyPort, 0, 65535),
                Username = proxyUser,
                Password = proxyPass,
                Type = proxyType,
                Version = version
            }
        };

        Applied = true;
        Close();
    }

    private void LoadFromGlobalConfig()
    {
        var bindIp = UBot.Core.RuntimeAccess.Global.Get("UBot.Network.BindIp", "0.0.0.0");
        var proxy = UBot.Core.RuntimeAccess.Global.GetArray<string>("UBot.Network.Proxy", '|', StringSplitOptions.TrimEntries);

        var active = false;
        var proxyIp = string.Empty;
        var proxyPort = "0";
        var proxyUser = string.Empty;
        var proxyPass = string.Empty;
        var proxyType = "SOCKS5";

        if (proxy.Length >= 6)
        {
            _ = bool.TryParse(proxy[0], out active);
            proxyIp = proxy[1];
            proxyPort = proxy[2];
            proxyUser = proxy[3];
            proxyPass = proxy[4];
            proxyType = proxy[5] == "4" ? "SOCKS4" : "SOCKS5";
        }

        ProxyActiveToggle.IsChecked = active;
        ProxyIpBox.Text = proxyIp;
        ProxyPortBox.Text = proxyPort;
        ProxyUserBox.Text = proxyUser;
        ProxyPassBox.Text = proxyPass;
        ProxyTypeSelect.SelectedItem = proxyType;
        IpBindBox.Text = string.IsNullOrWhiteSpace(bindIp) ? "0.0.0.0" : bindIp;
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }
}
