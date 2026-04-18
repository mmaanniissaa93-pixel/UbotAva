using Avalonia.Controls;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Logging;

public partial class LogFeatureView : UserControl
{
    public LogFeatureView()
    {
        InitializeComponent();
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        LogItems.ItemsSource = state.LogLines;
    }
}
