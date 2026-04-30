using System;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Statistics.Stats.Calculators.Static;

internal class Loot : IStatisticCalculator
{
    /// <inheritdoc />
    private int _currentValue;

    /// <inheritdoc />
    public string Name => "ItemsPicked";

    /// <inheritdoc />
    public string Label => LanguageManager.GetLang("Calculators.Loot.Label");

    /// <inheritdoc />
    public StatisticsGroup Group => StatisticsGroup.Loot;

    /// <inheritdoc />
    public string ValueFormat => "{0}";

    /// <inheritdoc />
    public UpdateType UpdateType => UpdateType.Static;

    /// <inheritdoc />
    public object GetValue()
    {
        return _currentValue;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _currentValue = 0;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnPickupItem", new Action<InventoryItem>(OnPickupItem));
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnPartyPickItem", new Action<InventoryItem>(OnPickupItem));
    }

    /// <summary>
    ///     Called when [pickup item].
    /// </summary>
    /// <param name="item">The item.</param>
    private void OnPickupItem(InventoryItem item)
    {
        _currentValue++;
    }
}
