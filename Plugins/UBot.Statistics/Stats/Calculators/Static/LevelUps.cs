using UBot.Core;
using UBot.Core.Components;

namespace UBot.Statistics.Stats.Calculators.Static;

internal class LevelUps : IStatisticCalculator
{
    /// <summary>
    ///     The initial value
    /// </summary>
    private uint _initialValue;

    /// <inheritdoc />
    public string Name => "LevelUps";

    /// <inheritdoc />
    public string Label => LanguageManager.GetLang("Calculators.LevelUps.Label");

    /// <inheritdoc />
    public StatisticsGroup Group => StatisticsGroup.Player;

    /// <inheritdoc />
    public string ValueFormat => "{0}";

    /// <inheritdoc />
    public UpdateType UpdateType => UpdateType.Static;

    /// <inheritdoc />
    public object GetValue()
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready)
            return 0;

        return UBot.Core.RuntimeAccess.Session.Player.Level - (double)_initialValue;
    }

    /// <inheritdoc />
    public void Reset()
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready)
            return;

        _initialValue = UBot.Core.RuntimeAccess.Session.Player.Level;
    }

    /// <inheritdoc />
    public void Initialize() { }
}
