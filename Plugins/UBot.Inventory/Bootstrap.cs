using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;
using UBot.Inventory.Subscriber;

namespace UBot.Inventory;

public class Bootstrap : IPlugin
{
    /// <inheritdoc />
    public string Author => "UBot Team";

    /// <inheritdoc />
    public string Description => "Provides inventory management features, including item usage and purchasing.";

    /// <inheritdoc />
    public string Name => "UBot.Inventory";

    /// <inheritdoc />
    public string Title => "Inventory";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public bool DisplayAsTab => true;

    /// <inheritdoc />
    public int Index => 4;

    /// <inheritdoc />
    public bool RequireIngame => true;

    /// <inheritdoc />
    public void Initialize()
    {
        BuyItemSubscriber.SubscribeEvents();
        InventoryUpdateSubscriber.SubscribeEvents();
        UseItemAtTrainplaceSubscriber.SubscribeEvents();
    }

    /// <inheritdoc />
    public Control View => Views.View.Instance;

    /// <inheritdoc />
    public void Translate()
    {
        LanguageManager.Translate(View, UBot.Core.RuntimeAccess.Core.Language);
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

