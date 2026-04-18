using Avalonia.Controls;
using System.Text.Json;
using UBot.Avalonia.ViewModels;
using UBot.Avalonia.Services;

namespace UBot.Avalonia.Features.TargetAssist;

public partial class TargetAssistFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private AppState? _state;

    public TargetAssistFeatureView() { InitializeComponent(); }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm; _state = state;
        Build();
    }

    public void UpdateFromState(JsonElement moduleState) => Build();

    private void Build()
    {
        if (_vm is null) return;
        ContentHost.Children.Clear();
        ContentHost.Children.Add(new TextBlock
        {
            Text = "TargetAssist â€” feature view (full implementation wired via FeatureViewFactory)",
            Classes = { "form-label" },
            Margin = new global::Avalonia.Thickness(0, 8)
        });
    }
}

