using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Plugins;
using UBot.Quest.Views.Sidebar;

namespace UBot.Quest;

public class QuestPlugin : IPlugin
{
    /// <inheritdoc />
    public string Author => "UBot Team";

    /// <inheritdoc />
    public string Description => "A plugin that provides a quest log for tracking quests and their progress.";

    /// <inheritdoc />
    public string Name => "UBot.QuestLog";
    
    /// <inheritdoc />
    public string Title => "Quests";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public bool DisplayAsTab => false;

    /// <inheritdoc />
    public int Index => 0;

    /// <inheritdoc />
    public bool RequireIngame => true;

    /// <inheritdoc />
    public Control View => Views.View.Main;

    /// <inheritdoc />
    public void Translate()
    {
        LanguageManager.Translate(View, Kernel.Language);
    }

    /// <inheritdoc />
    public void Initialize()
    {
        Views.View.SidebarElement = new QuestSidebarElement();

        EventManager.FireEvent("OnAddSidebarElement", Views.View.SidebarElement);
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

