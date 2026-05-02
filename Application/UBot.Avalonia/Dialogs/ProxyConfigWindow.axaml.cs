using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Net;
using UBot.Avalonia.Services;

namespace UBot.Avalonia.Dialogs;

public partial class ProxyConfigWindow : Window
{
    private readonly IUbotCoreService _core;

    public NetworkConfig? Config { get; private set; }
    public bool Applied { get; private set; }

    public ProxyConfigWindow(IUbotCoreService core)
    {
        _core = core;
        InitializeComponent();
        ProxyTypeSelect.ItemsSource = new[] { "SOCKS5", "SOCKS4" };
        Opened += async (_, _) => await LoadFromServiceAsync();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        var bindIp = (IpBindBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(bindIp))
            bindIp = "0.0.0.0";

        if (!IPAddress.TryParse(bindIp, out _))
            return;

        var active    = ProxyActiveToggle.IsChecked == true;
        var proxyIp   = (ProxyIpBox.Text ?? string.Empty).Trim();
        var proxyUser = (ProxyUserBox.Text ?? string.Empty).Trim();
        var proxyPass = ProxyPassBox.Text ?? string.Empty;
        var proxyType = ProxyTypeSelect.SelectedItem?.ToString()?.Trim().ToUpperInvariant() ?? "SOCKS5";
        var version   = proxyType == "SOCKS4" ? 4 : 5;
        var proxyPort = ParseInt(ProxyPortBox.Text, 0);

        if (active)
        {
            if (string.IsNullOrWhiteSpace(proxyIp))
                return;
            if (proxyPort <= 0 || proxyPort > 65535)
                return;
        }

        var networkConfig = new NetworkConfig
        {
            BindIp = bindIp,
            Proxy  = new ProxyConfig
            {
                Active   = active,
                Ip       = proxyIp,
                Port     = Math.Clamp(proxyPort, 0, 65535),
                Username = proxyUser,
                Password = proxyPass,
                Type     = proxyType,
                Version  = version
            }
        };

        try
        {
            await _core.SaveNetworkConfigAsync(networkConfig);
        }
        catch
        {
            // Save hatası dialog'u kapatmamalı.
            return;
        }

        Config  = networkConfig;
        Applied = true;
        Close();
    }

    private async System.Threading.Tasks.Task LoadFromServiceAsync()
    {
        try
        {
            var cfg = await _core.GetNetworkConfigAsync();

            ProxyActiveToggle.IsChecked  = cfg.Proxy.Active;
            ProxyIpBox.Text              = cfg.Proxy.Ip;
            ProxyPortBox.Text            = cfg.Proxy.Port.ToString(CultureInfo.InvariantCulture);
            ProxyUserBox.Text            = cfg.Proxy.Username;
            ProxyPassBox.Text            = cfg.Proxy.Password;
            ProxyTypeSelect.SelectedItem = cfg.Proxy.Type;
            IpBindBox.Text               = string.IsNullOrWhiteSpace(cfg.BindIp) ? "0.0.0.0" : cfg.BindIp;
        }
        catch
        {
            // Dialog açık kalmalı; kontroller varsayılan değerlerini korur.
        }
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }
}
