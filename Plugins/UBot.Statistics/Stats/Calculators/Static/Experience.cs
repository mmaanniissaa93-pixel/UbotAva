using System;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;

namespace UBot.Statistics.Stats.Calculators.Static;

internal class Experience : IStatisticCalculator
{
    /// <summary>
    ///     The initial level
    /// </summary>
    private byte _initialLevel;

    /// <summary>
    ///     The initial offset
    /// </summary>
    private double _initialOffset;

    /// <summary>
    ///     The initial value
    /// </summary>
    private double _initialValue;

    /// <inheritdoc />
    public string Name => "EXPGained";

    /// <inheritdoc />
    public string Label => LanguageManager.GetLang("Calculators.Experience.Label");

    /// <inheritdoc />
    public StatisticsGroup Group => StatisticsGroup.Player;

    /// <inheritdoc />
    public string ValueFormat => "{0} %";

    /// <inheritdoc />
    public UpdateType UpdateType => UpdateType.Static;

    /// <inheritdoc />
    public object GetValue()
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready)
            return 0;

        var levelDifference = UBot.Core.RuntimeAccess.Session.Player.Level - _initialLevel;

        double gainedExpPercent = 0;
        var offset = _initialOffset;
        if (levelDifference >= 1)
            for (var i = levelDifference; i > 0; i--)
            {
                gainedExpPercent += 100 - offset;
                offset = 0;
            }

        gainedExpPercent +=
            UBot.Core.RuntimeAccess.Session.Player.Experience / (double)UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefLevel(UBot.Core.RuntimeAccess.Session.Player.Level).Exp_C * 100 - offset;

        return Math.Round(gainedExpPercent, 2);
    }

    /// <inheritdoc />
    public void Reset()
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready)
            return;

        //EXP Percent
        _initialValue =
            UBot.Core.RuntimeAccess.Session.Player.Experience / (double)UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefLevel(UBot.Core.RuntimeAccess.Session.Player.Level).Exp_C * 100;

        _initialOffset = _initialValue;

        _initialLevel = UBot.Core.RuntimeAccess.Session.Player.Level;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnLevelUp", OnLevelUp);
    }

    private void OnLevelUp()
    {
        //EXP Percent
        _initialValue =
            UBot.Core.RuntimeAccess.Session.Player.Experience / (double)UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefLevel(UBot.Core.RuntimeAccess.Session.Player.Level).Exp_C * 100;
    }
}
