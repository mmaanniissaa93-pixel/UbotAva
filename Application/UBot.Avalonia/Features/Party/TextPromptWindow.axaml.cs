using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace UBot.Avalonia.Features.Party;

public partial class TextPromptWindow : Window
{
    public TextPromptWindow()
    {
        InitializeComponent();
        InputBox.KeyDown += InputBox_KeyDown;
    }

    public TextPromptWindow(string title, string label, string placeholder)
        : this()
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Input" : title;
        PromptLabel.Text = string.IsNullOrWhiteSpace(label) ? "Value" : label;
        InputBox.Watermark = placeholder;
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        InputBox.Focus();
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var text = (InputBox.Text ?? string.Empty).Trim();
        Close(string.IsNullOrWhiteSpace(text) ? null : text);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void InputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Ok_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Cancel_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}
