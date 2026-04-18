using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;
using UBot.AutoDungeon.Views;

namespace UBot.AutoDungeon;

public class AutoDungeonPlugin : IPlugin
{
    private AttackAreaScriptCommand _attackAreaCommand;
    private GoDimensionalScriptCommand _goDimensionalCommand;

    public string Author => "UBot Team";
    public string Description => "Automates dungeon-entry and area-clear workflows with configurable monster filters.";
    public string Name => "UBot.AutoDungeon";
    public string Title => "Auto Dungeon";
    public string Version => "0.1.0";
    public bool Enabled { get; set; }
    public bool DisplayAsTab => false;
    public int Index => 120;
    public bool RequireIngame => false;

    public void Initialize()
    {
        AutoDungeonState.LoadFromConfig();

        _attackAreaCommand = new AttackAreaScriptCommand();
        _goDimensionalCommand = new GoDimensionalScriptCommand();

        ScriptManager.RegisterCommandHandler(_attackAreaCommand);
        ScriptManager.RegisterCommandHandler(_goDimensionalCommand);
    }

    public Control View => UBot.AutoDungeon.Views.View.Instance;

    public void Translate()
    {
        LanguageManager.Translate(View, Kernel.Language);
    }

    public void OnLoadCharacter()
    {
        AutoDungeonState.LoadFromConfig();
        if (View is AutoDungeonView ui)
            ui.ReloadFromState();
    }

    public void Enable()
    {
        AutoDungeonState.LoadFromConfig();

        if (_attackAreaCommand != null)
            ScriptManager.RegisterCommandHandler(_attackAreaCommand);

        if (_goDimensionalCommand != null)
            ScriptManager.RegisterCommandHandler(_goDimensionalCommand);

        if (View != null)
            View.Enabled = true;
    }

    public void Disable()
    {
        AutoDungeonState.ClearRuntimeState();

        ScriptManager.UnregisterCommandHandler("AttackArea");
        ScriptManager.UnregisterCommandHandler("GoDimensional");

        if (View != null)
            View.Enabled = false;
    }
}

