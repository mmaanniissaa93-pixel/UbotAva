using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;
using UBot.General.Components;

namespace UBot.General;

public class Bootstrap : IPlugin
{
    /// <inheritdoc />
    public string Author => "UBot Team";

    /// <inheritdoc />
    public string Description => "General plugin for UBot, providing various utilities and features.";

    /// <inheritdoc />
    public string Name => "UBot.General";

    /// <inheritdoc />
    public string Title => "General";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public bool DisplayAsTab => true;

    /// <inheritdoc />
    public int Index => 0;

    /// <inheritdoc />
    public bool RequireIngame => false;

    /// <inheritdoc />
    public void Initialize()
    {
        Accounts.Load();
        AutoLoginRuntimeFeatures.Initialize();
    }

    /// <inheritdoc />
    public Control View => Views.View.Instance;

    /// <inheritdoc />
    public void Translate()
    {
        LanguageManager.Translate(View, Kernel.Language);
        LanguageManager.Translate(Views.View.PendingWindow, Kernel.Language);
        LanguageManager.Translate(Views.View.AccountsWindow, Kernel.Language);
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

