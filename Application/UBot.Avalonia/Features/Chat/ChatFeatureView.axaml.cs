using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Chat;

public partial class ChatFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private string _activeChannel = "all";
    private readonly ObservableCollection<string> _lines = new();

    private static readonly (string Id, string Label)[] Tabs = {
        ("all","All"),("private","Private"),("party","Party"),
        ("guild","Guild"),("global","Global / Notice"),("stall","Stall"),("unique","Unique")
    };

    public ChatFeatureView()
    {
        InitializeComponent();
        ChatLog.ItemsSource = _lines;
        ChatTabs.SetTabs(Tabs);
        ChatTabs.TabChanged += t =>
        {
            _activeChannel = t switch {
                "all"     => "all",
                "private" => "private",
                "party"   => "party",
                "guild"   => "guild",
                "global"  => "global",
                "stall"   => "stall",
                _         => "all"
            };
            TargetBox.IsVisible = t == "private";
            ChannelLabel.Text   = $"Channel: {_activeChannel}";
        };
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm;
        ChannelLabel.Text = "Channel: all";
        state.LogLines.CollectionChanged += (_, _) => Sync(state);
        Sync(state);
    }

    private void Sync(AppState state)
    {
        _lines.Clear();
        foreach (var l in state.LogLines) _lines.Add(l);
    }

    private void Send_Click(object? s, RoutedEventArgs e)
    {
        if (_vm is null) return;
        var msg = MessageBox.Text?.Trim();
        if (string.IsNullOrEmpty(msg)) return;
        _ = _vm.PluginActionAsync("chat.send", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["channel"] = _activeChannel,
            ["message"] = msg,
            ["target"]  = TargetBox.Text?.Trim() ?? ""
        });
        MessageBox.Text = "";
    }
}
