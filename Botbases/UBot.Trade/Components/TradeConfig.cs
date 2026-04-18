using System.Collections.Generic;
using System.IO;
using System.Linq;
using UBot.Core;

namespace UBot.Trade.Components;

internal static class TradeConfig
{
    public static string TracePlayerName
    {
        get => PlayerConfig.Get("UBot.Trade.TracePlayerName", "");
        set => PlayerConfig.Set("UBot.Trade.TracePlayerName", value);
    }

    public static bool TracePlayer
    {
        get => PlayerConfig.Get("UBot.Trade.TracePlayer", false);
        set => PlayerConfig.Set("UBot.Trade.TracePlayer", value);
    }

    public static bool MountTransport
    {
        get => PlayerConfig.Get("UBot.Trade.MountTransport", false);
        set => PlayerConfig.Set("UBot.Trade.MountTransport", value);
    }

    public static bool UseRouteScripts
    {
        get => PlayerConfig.Get("UBot.Trade.UseRouteScripts", true);
        set => PlayerConfig.Set("UBot.Trade.UseRouteScripts", value);
    }

    public static int SelectedRouteListIndex
    {
        get => PlayerConfig.Get("UBot.Trade.SelectedRouteListIndex", 0);
        set => PlayerConfig.Set("UBot.Trade.SelectedRouteListIndex", value);
    }

    public static bool RunTownScript
    {
        get => PlayerConfig.Get("UBot.Trade.RunTownScript", false);
        set => PlayerConfig.Set("UBot.Trade.RunTownScript", value);
    }

    public static bool WaitForHunter
    {
        get => PlayerConfig.Get("UBot.Trade.WaitForHunter", false);
        set => PlayerConfig.Set("UBot.Trade.WaitForHunter", value);
    }

    public static bool AttackThiefPlayers
    {
        get => PlayerConfig.Get("UBot.Trade.AttackThiefPlayers", false);
        set => PlayerConfig.Set("UBot.Trade.AttackThiefPlayers", value);
    }

    public static bool AttackThiefNpcs
    {
        get => PlayerConfig.Get("UBot.Trade.AttackThiefNpcs", false);
        set => PlayerConfig.Set("UBot.Trade.AttackThiefNpcs", value);
    }

    public static bool CastBuffs
    {
        get => PlayerConfig.Get("UBot.Trade.CastBuffs", false);
        set => PlayerConfig.Set("UBot.Trade.CastBuffs", value);
    }

    public static bool CounterAttack
    {
        get => PlayerConfig.Get("UBot.Trade.CounterAttack", false);
        set => PlayerConfig.Set("UBot.Trade.CounterAttack", value);
    }

    public static bool ProtectTransport
    {
        get => PlayerConfig.Get("UBot.Trade.ProtectTransport", false);
        set => PlayerConfig.Set("UBot.Trade.ProtectTransport", value);
    }

    public static bool BuyGoods
    {
        get => PlayerConfig.Get("UBot.Trade.BuyGoods", true);
        set => PlayerConfig.Set("UBot.Trade.BuyGoods", value);
    }

    public static bool SellGoods
    {
        get => PlayerConfig.Get("UBot.Trade.SellGoods", true);
        set => PlayerConfig.Set("UBot.Trade.SellGoods", value);
    }

    public static int BuyGoodsQuantity
    {
        get => PlayerConfig.Get("UBot.Trade.BuyGoodsQuantity", 0);
        set => PlayerConfig.Set("UBot.Trade.BuyGoodsQuantity", value);
    }

    public static int MaxTransportDistance
    {
        get => PlayerConfig.Get("UBot.Trade.MaxTransportDistance", 15);
        set => PlayerConfig.Set("UBot.Trade.MaxTransportDistance", value == 0 ? 1 : value);
    }

    public static List<string> RouteScriptList
    {
        get
        {
            var result = PlayerConfig.GetArray<string>("UBot.Trade.RouteScriptList", ';').ToList();

            if (!result.Contains("Default"))
                result.Add("Default");

            return result;
        }
        set => PlayerConfig.SetArray("UBot.Trade.RouteScriptList", value, ";");
    }

    public static Dictionary<string, List<string>> RouteScripts
    {
        get
        {
            var result = new Dictionary<string, List<string>>(16);

            foreach (var scriptList in RouteScriptList)
            {
                var scripts =
                    PlayerConfig
                        .GetArray<string>($"UBot.Trade.RouteScriptList.{scriptList}")
                        .Where(File.Exists)
                        .ToList()
                    ?? new List<string>();

                result.Add(scriptList, scripts);
            }

            return result;
        }
        set
        {
            foreach (var scriptList in value)
                PlayerConfig.SetArray($"UBot.Trade.RouteScriptList.{scriptList.Key}", scriptList.Value);

            RouteScriptList = value.Keys.ToList();
        }
    }
}
