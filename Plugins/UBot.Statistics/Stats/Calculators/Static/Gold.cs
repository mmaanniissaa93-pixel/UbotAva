using UBot.Core;
using UBot.Core.Components;

namespace UBot.Statistics.Stats.Calculators.Static;

internal class Gold : IStatisticCalculator
{
    /// <summary>
    ///     The initial value
    /// </summary>
    private ulong _initialValue;

    /// <inheritdoc />
    public string Name => "GoldGained";

    /// <inheritdoc />
    public string Label => LanguageManager.GetLang("Calculators.Gold.Label");

    /// <inheritdoc />
    public StatisticsGroup Group => StatisticsGroup.Loot;

    /// <inheritdoc />
    public string ValueFormat => "{0}";

    /// <inheritdoc />
    public UpdateType UpdateType => UpdateType.Static;

    /// <inheritdoc />
    public object GetValue()
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready)
            return 0;

        return UBot.Core.RuntimeAccess.Session.Player.Gold - (double)_initialValue;
    }

    /// <inheritdoc />
    public void Reset()
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready)
            return;

        _initialValue = UBot.Core.RuntimeAccess.Session.Player.Gold;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        //Nothing to do here!
    }
}
