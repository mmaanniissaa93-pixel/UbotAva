using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace UBot.Avalonia.Dialogs;

public partial class ProxyConfigWindow : Window
{
    public ProxyConfigWindow()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
