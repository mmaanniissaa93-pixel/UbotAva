using UBot.Core;
using UBot.Core.Components;

namespace UBot.Statistics.Stats.Calculators.Static;

internal class SkillPoints : IStatisticCalculator
{
    /// <summary>
    ///     The initial value
    /// </summary>
    private long _initialValue;

    /// <inheritdoc />
    public string Name => "SPGained";

    /// <inheritdoc />
    public string Label => LanguageManager.GetLang("Calculators.SkillPoints.Label");

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

        return UBot.Core.RuntimeAccess.Session.Player.SkillPoints - _initialValue;
    }

    /// <inheritdoc />
    public void Reset()
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready)
            return;

        _initialValue = UBot.Core.RuntimeAccess.Session.Player.SkillPoints;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        //Nothing to do here!
    }
}
