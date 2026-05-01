using System;
using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;
using UBot.Protection.Components.Pet;
using UBot.Protection.Components.Player;
using UBot.Protection.Components.Town;

namespace UBot.Protection;

public class Bootstrap : IPlugin
{
    /// <inheritdoc />
    public string Author => "UBot Team";

    /// <inheritdoc />
    public string Description => "Provides various features to protect your character while botting, such as auto-healing, and more...";

    /// <inheritdoc />
    public string Name => "UBot.Protection";

    /// <inheritdoc />
    public string Title => "Protection";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public bool DisplayAsTab => true;

    /// <inheritdoc />
    public int Index => 2;

    /// <inheritdoc />
    public bool RequireIngame => true;

    /// <inheritdoc />
    public void Initialize()
    {
        //Player handlers
        HealthManaRecoveryHandler.Initialize();
        UniversalPillHandler.Initialize();
        VigorRecoveryHandler.Initialize();
        StatPointsHandler.Initialize();

        //Start-time return-to-town precheck coordinator
        StartPrecheckHandler.Initialize();

        //Pet handlers
        CosHealthRecoveryHandler.Initialize();
        CosHGPRecoveryHandler.Initiliaze();
        CosBadStatusHandler.Initialize();
        CosReviveHandler.Initialize();
        AutoSummonAttackPet.Initialize();

        //Back town
        DeadHandler.Initialize();
        AmmunitionHandler.Initialize();
        InventoryFullHandler.Initialize();
        PetInventoryFullHandler.Initialize();
        NoManaPotionsHandler.Initialize();
        NoHealthPotionsHandler.Initialize();
        LevelUpHandler.Initialize();
        DurabilityLowHandler.Initialize();
        FatigueHandler.Initialize();
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
        HealthManaRecoveryHandler.SubscribeAll();
        UniversalPillHandler.SubscribeAll();
        VigorRecoveryHandler.SubscribeAll();
        StatPointsHandler.SubscribeAll();
        StartPrecheckHandler.SubscribeAll();
        CosHealthRecoveryHandler.SubscribeAll();
        CosHGPRecoveryHandler.SubscribeAll();
        CosBadStatusHandler.SubscribeAll();
        CosReviveHandler.SubscribeAll();
        AutoSummonAttackPet.SubscribeAll();
        DeadHandler.SubscribeAll();
        AmmunitionHandler.SubscribeAll();
        InventoryFullHandler.SubscribeAll();
        PetInventoryFullHandler.SubscribeAll();
        NoManaPotionsHandler.SubscribeAll();
        NoHealthPotionsHandler.SubscribeAll();
        LevelUpHandler.SubscribeAll();
        DurabilityLowHandler.SubscribeAll();
        FatigueHandler.SubscribeAll();

        if (View != null)
            View.Enabled = true;
    }

    /// <inheritdoc />
    public void Disable()
    {
        TryUnsubscribe(CosHealthRecoveryHandler.UnsubscribeAll);
        TryUnsubscribe(UniversalPillHandler.UnsubscribeAll);
        TryUnsubscribe(HealthManaRecoveryHandler.UnsubscribeAll);
        TryUnsubscribe(CosBadStatusHandler.UnsubscribeAll);
        TryUnsubscribe(VigorRecoveryHandler.UnsubscribeAll);
        TryUnsubscribe(StatPointsHandler.UnsubscribeAll);
        TryUnsubscribe(StartPrecheckHandler.UnsubscribeAll);
        TryUnsubscribe(CosHGPRecoveryHandler.UnsubscribeAll);
        TryUnsubscribe(CosReviveHandler.UnsubscribeAll);
        TryUnsubscribe(AutoSummonAttackPet.UnsubscribeAll);
        TryUnsubscribe(DeadHandler.UnsubscribeAll);
        TryUnsubscribe(AmmunitionHandler.UnsubscribeAll);
        TryUnsubscribe(InventoryFullHandler.UnsubscribeAll);
        TryUnsubscribe(PetInventoryFullHandler.UnsubscribeAll);
        TryUnsubscribe(NoManaPotionsHandler.UnsubscribeAll);
        TryUnsubscribe(NoHealthPotionsHandler.UnsubscribeAll);
        TryUnsubscribe(LevelUpHandler.UnsubscribeAll);
        TryUnsubscribe(DurabilityLowHandler.UnsubscribeAll);
        TryUnsubscribe(FatigueHandler.UnsubscribeAll);

        if (View != null)
            View.Enabled = false;
    }

    private void TryUnsubscribe(Action unsubscribeAction)
    {
        try
        {
            unsubscribeAction?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error($"Error during handler cleanup: {ex.Message}");
        }
    }
}

