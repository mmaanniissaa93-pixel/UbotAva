using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.General;

public partial class AccountSetupWindow : Window
{
    private readonly GeneralViewModel? _vm;
    private readonly ObservableCollection<AutoLoginAccountDto> _accounts = new();

    public AccountSetupWindow()
    {
        InitializeComponent();
        AccountsList.ItemsSource = _accounts;
    }

    public AccountSetupWindow(GeneralViewModel vm)
        : this()
    {
        _vm = vm;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        await ReloadAccountsAsync();
    }

    private async System.Threading.Tasks.Task ReloadAccountsAsync()
    {
        if (_vm == null)
            return;

        var accounts = await _vm.LoadAutoLoginAccountsAsync();

        _accounts.Clear();
        foreach (var account in accounts.OrderBy(x => x.Username, StringComparer.OrdinalIgnoreCase))
            _accounts.Add(Clone(account));

        InfoText.Text = _accounts.Count == 0 ? "No accounts." : $"{_accounts.Count} account(s) loaded.";
        StatusText.Text = string.Empty;
    }

    private async void AddOrUpdate_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        var username = (UsernameBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            StatusText.Text = "Username is required.";
            return;
        }

        var serverName = (ServerNameBox.Text ?? string.Empty).Trim();
        var password = PasswordBox.Text ?? string.Empty;
        var secondaryPassword = SecondaryPasswordBox.Text ?? string.Empty;

        var existing = _accounts.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            _accounts.Add(new AutoLoginAccountDto
            {
                Username = username,
                Password = password,
                SecondaryPassword = secondaryPassword,
                ServerName = serverName,
                Type = "Joymax",
                Channel = 1,
                Characters = new List<string>()
            });
        }
        else
        {
            existing.Password = password;
            existing.SecondaryPassword = secondaryPassword;
            existing.ServerName = serverName;
            existing.Type = string.IsNullOrWhiteSpace(existing.Type) ? "Joymax" : existing.Type;
            if (existing.Channel == 0)
                existing.Channel = 1;
        }

        if (!await _vm.SaveAutoLoginAccountsAsync(_accounts.ToList()))
        {
            StatusText.Text = "Failed to save accounts.";
            return;
        }

        await ReloadAccountsAsync();
        SelectByUsername(username);
        StatusText.Text = "Account saved.";
    }

    private async void RemoveSelected_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        if (AccountsList.SelectedItem is not AutoLoginAccountDto selected)
        {
            StatusText.Text = "Select an account to remove.";
            return;
        }

        var target = _accounts.FirstOrDefault(x => string.Equals(x.Username, selected.Username, StringComparison.OrdinalIgnoreCase));
        if (target == null)
            return;

        _accounts.Remove(target);
        if (!await _vm.SaveAutoLoginAccountsAsync(_accounts.ToList()))
        {
            StatusText.Text = "Failed to save account removal.";
            return;
        }

        await ReloadAccountsAsync();
        ClearForm();
        StatusText.Text = "Account removed.";
    }

    private void AccountsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AccountsList.SelectedItem is not AutoLoginAccountDto selected)
            return;

        UsernameBox.Text = selected.Username;
        PasswordBox.Text = selected.Password;
        SecondaryPasswordBox.Text = selected.SecondaryPassword;
        ServerNameBox.Text = selected.ServerName;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e) => Close(true);

    private void SelectByUsername(string username)
    {
        AccountsList.SelectedItem = _accounts.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    private void ClearForm()
    {
        UsernameBox.Text = string.Empty;
        PasswordBox.Text = string.Empty;
        SecondaryPasswordBox.Text = string.Empty;
        ServerNameBox.Text = string.Empty;
    }

    private static AutoLoginAccountDto Clone(AutoLoginAccountDto account)
    {
        return new AutoLoginAccountDto
        {
            Username = account.Username,
            Password = account.Password,
            SecondaryPassword = account.SecondaryPassword,
            Channel = account.Channel,
            Type = account.Type,
            ServerName = account.ServerName,
            SelectedCharacter = account.SelectedCharacter,
            Characters = account.Characters?.ToList() ?? new List<string>()
        };
    }
}
