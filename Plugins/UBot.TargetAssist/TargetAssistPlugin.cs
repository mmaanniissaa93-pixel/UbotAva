using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;

namespace UBot.TargetAssist;

public class TargetAssistPlugin : IPlugin
{
    public string Author => "UBot Team";

    public string Description => "Selects nearby valid player targets with safe retarget debounce.";

    public string Name => "UBot.TargetAssist";

    public string Title => "Target Assist";

    public string Version => "0.1.0";

    public bool Enabled { get; set; }

    public bool DisplayAsTab => true;

    public int Index => 130;

    public bool RequireIngame => true;

    public void Initialize()
    {
        TargetAssistConfig.EnsureDefaults();
        TargetAssistRuntime.Initialize();
        TargetAssistRuntime.SetActive(true);
    }

    public Control View => Views.View.Instance;

    public void Translate()
    {
        LanguageManager.Translate(View, UBot.Core.RuntimeAccess.Core.Language);
    }

    public void OnLoadCharacter()
    {
        TargetAssistConfig.EnsureDefaults();
        TargetAssistRuntime.Reset();
    }

    public void Enable()
    {
        TargetAssistRuntime.SetActive(true);
    }

    public void Disable()
    {
        TargetAssistRuntime.UnsubscribeAll();
        TargetAssistRuntime.SetActive(false);
        TargetAssistRuntime.Reset();
    }
}
