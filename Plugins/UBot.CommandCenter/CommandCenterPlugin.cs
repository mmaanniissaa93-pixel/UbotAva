using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;

namespace UBot.CommandCenter;

public class CommandCenterPlugin : IPlugin
{
    /// <inheritdoc />
    public string Author => "UBot Team";

    /// <inheritdoc />
    public string Description => "A plugin that provides a command center for managing your bot.";

    /// <inheritdoc />
    public string Name => "UBot.CommandCenter";

    /// <inheritdoc />
    public string Title => "Command center";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public bool DisplayAsTab => false;

    /// <inheritdoc />
    public int Index => 100;

    /// <inheritdoc />
    public bool RequireIngame => true;

    /// <inheritdoc />
    public void Initialize()
    {
        Log.Notify("[Command Center] Plugin initialized!");
    }

    /// <inheritdoc />
    public Control View => Views.View.Main;

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

