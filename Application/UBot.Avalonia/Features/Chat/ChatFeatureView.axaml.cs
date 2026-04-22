using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Chat;

public partial class ChatFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private AppState? _state;
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
                "unique"  => "unique",
                _         => "all"
            };
            TargetBox.IsVisible = t == "private";
            ChannelLabel.Text   = $"Channel: {_activeChannel}";
            Sync();
        };
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm;
        _state = state;
        ChannelLabel.Text = "Channel: all";
        state.ChatMessages.CollectionChanged += (_, _) => Sync();
        Sync();
    }

    private void Sync()
    {
        if (_state == null)
            return;

        _lines.Clear();
        foreach (var entry in _state.ChatMessages)
        {
            if (!ShouldInclude(entry.Channel))
                continue;

            _lines.Add(entry.DisplayText);
        }
    }

    private bool ShouldInclude(string channel)
    {
        if (_activeChannel == "all")
            return true;

        return string.Equals(channel, _activeChannel, System.StringComparison.OrdinalIgnoreCase);
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
