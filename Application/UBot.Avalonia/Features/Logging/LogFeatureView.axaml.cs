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
        BindFilterControls();
    }

    private void BindFilterControls()
    {
        if (_state == null) return;

        if (ShowDebugToggle != null)
            ShowDebugToggle.IsChecked = _state.ShowDebug;
        if (ShowEntityToggle != null)
            ShowEntityToggle.IsChecked = _state.ShowEntity;
        if (ShowPerfToggle != null)
            ShowPerfToggle.IsChecked = _state.ShowPerf;
        if (ShowProtocolToggle != null)
            ShowProtocolToggle.IsChecked = _state.ShowProtocol;
        if (ShowErrorsOnlyToggle != null)
            ShowErrorsOnlyToggle.IsChecked = _state.ShowErrorsOnly;
        if (PauseAutoscrollToggle != null)
            PauseAutoscrollToggle.IsChecked = _state.PauseAutoscroll;
        if (SearchTextBox != null)
            SearchTextBox.Text = _state.SearchFilter ?? string.Empty;
    }

    private void OnShowDebugToggled(object? sender, RoutedEventArgs e)
    {
        if (_state != null && sender is CheckBox cb)
            _state.ShowDebug = cb.IsChecked ?? false;
    }

    private void OnShowEntityToggled(object? sender, RoutedEventArgs e)
    {
        if (_state != null && sender is CheckBox cb)
            _state.ShowEntity = cb.IsChecked ?? false;
    }

    private void OnShowPerfToggled(object? sender, RoutedEventArgs e)
    {
        if (_state != null && sender is CheckBox cb)
            _state.ShowPerf = cb.IsChecked ?? false;
    }

    private void OnShowProtocolToggled(object? sender, RoutedEventArgs e)
    {
        if (_state != null && sender is CheckBox cb)
            _state.ShowProtocol = cb.IsChecked ?? false;
    }

    private void OnShowErrorsOnlyToggled(object? sender, RoutedEventArgs e)
    {
        if (_state != null && sender is CheckBox cb)
            _state.ShowErrorsOnly = cb.IsChecked ?? false;
    }

    private void OnPauseAutoscrollToggled(object? sender, RoutedEventArgs e)
    {
        if (_state != null && sender is CheckBox cb)
            _state.PauseAutoscroll = cb.IsChecked ?? false;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_state != null && sender is TextBox tb)
            _state.SearchFilter = tb.Text ?? string.Empty;
    }

    private void ClearLogs_Click(object? sender, RoutedEventArgs e)
    {
        _state?.ClearLogs();
    }
}
