using Avalonia.Controls;
using Avalonia.Interactivity;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Logging;

public partial class LogFeatureView : UserControl
{
    private AppState? _state;

    public LogFeatureView()
    {
        InitializeComponent();
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _state = state;
        LogItems.ItemsSource = state.LogLines;
    }

    private void ClearLogs_Click(object? sender, RoutedEventArgs e)
    {
        _state?.ClearLogs();
    }
}
