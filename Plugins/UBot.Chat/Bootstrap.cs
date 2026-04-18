using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;

namespace UBot.Chat;

public class Bootstrap : IPlugin
{
    /// <inheritdoc />
    public string Author => "UBot Team";

    /// <inheritdoc />
    public string Description => "Chat plugin for UBot.";

    /// <inheritdoc />
    public string Name => "UBot.Chat";

    /// <inheritdoc />
    public string Title => "Chat";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public bool DisplayAsTab => true;

    /// <inheritdoc />
    public int Index => 98;

    /// <inheritdoc />
    public bool RequireIngame => true;

    /// <inheritdoc />
    public void Initialize() { }

    /// <inheritdoc />
    public Control View => Views.View.Instance;

    /// <inheritdoc />
    public void Translate()
    {
        LanguageManager.Translate(View, Kernel.Language);
    }

    /// <inheritdoc />
    public void OnLoadCharacter()
    {
        // do nothing
    }

    /// <inheritdoc />
    public void Enable()
    {
        if (View != null)
            View.Enabled = true;
    }

    /// <inheritdoc />
    public void Disable()
    {
        if (View != null)
            View.Enabled = false;
    }
}

