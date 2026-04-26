using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;
using UBot.Skills.Subscriber;

namespace UBot.Skills;

public class Bootstrap : IPlugin
{
    /// <inheritdoc />
    public string Author => "UBot Team";

    /// <inheritdoc />
    public string Description => "A plugin that provides various protection features for the player and their pet.";

    /// <inheritdoc />
    public string Name => "UBot.Skills";

    /// <inheritdoc />
    public string Title => "Skills";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public bool DisplayAsTab => true;

    /// <inheritdoc />
    public int Index => 1;

    /// <inheritdoc />
    public bool RequireIngame => true;

    /// <inheritdoc />
    public void Initialize()
    {
        LoadCharacterSubscriber.SubscribeEvents();
    }

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
        LoadCharacterSubscriber.UnsubscribeAll();

        if (View != null)
            View.Enabled = false;
    }
}

