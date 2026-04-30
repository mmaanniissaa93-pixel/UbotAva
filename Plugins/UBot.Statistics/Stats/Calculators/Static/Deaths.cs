using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;

namespace UBot.Statistics.Stats.Calculators.Static;

internal class Deaths : IStatisticCalculator
{
    private int _deathsCounter;
    public string Name => "Deaths";
    public string Label => LanguageManager.GetLang("Calculators.Deaths.Label");
    public StatisticsGroup Group => StatisticsGroup.Player;
    public string ValueFormat => "{0}";
    public UpdateType UpdateType => UpdateType.Static;

    public object GetValue()
    {
        return _deathsCounter;
    }

    public void Reset()
    {
        _deathsCounter = 0;
    }

    public void Initialize()
    {
        SubscribeEvents();
    }

    private void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnPlayerDied", OnPlayerDead);
    }

    private void OnPlayerDead()
    {
        if (UBot.Core.RuntimeAccess.Core.Bot.Running)
            _deathsCounter++;
    }
}
