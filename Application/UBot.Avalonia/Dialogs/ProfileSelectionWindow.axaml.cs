using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace UBot.Avalonia.Dialogs;

public partial class ProfileSelectionWindow : Window
{
    public ProfileSelectionWindow()
    {
        InitializeComponent();
    }

    private void AddProfile_Click(object? sender, RoutedEventArgs e)
    {
        // Logic to add profile
    }

    private void DeleteProfile_Click(object? sender, RoutedEventArgs e)
    {
        // Logic to delete profile
    }

    private void Continue_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
