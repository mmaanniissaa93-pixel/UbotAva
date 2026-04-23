using Avalonia.Controls;
using Avalonia.Interactivity;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.CommandCenter;

public partial class CommandCenterFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private AppState? _state;
    private CommandCenterPopupWindow? _popup;

    public CommandCenterFeatureView()
    {
        InitializeComponent();
        OpenPopupBtn.Click += OpenPopupBtn_Click;
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm;
        _state = state;
    }

    public void UpdateFromState(System.Text.Json.JsonElement moduleState)
    {
        // Command center is config-driven, no live state stream needed.
    }

    public void OpenPopup(Window? owner)
    {
        if (_vm == null || _state == null)
            return;

        if (_popup is { IsVisible: true })
        {
            _popup.Activate();
            return;
        }

        _popup = new CommandCenterPopupWindow(_vm, _state)
        {
            WindowStartupLocation = owner != null
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen
        };
        _popup.Closed += (_, _) => _popup = null;

        if (owner != null)
        {
            _ = _popup.ShowDialog(owner);
        }
        else
        {
            _popup.Show();
        }
    }

    private void OpenPopupBtn_Click(object? sender, RoutedEventArgs e)
    {
        OpenPopup(this.VisualRoot as Window);
    }
}
