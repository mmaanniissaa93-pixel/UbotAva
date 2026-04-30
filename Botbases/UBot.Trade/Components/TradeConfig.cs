using System.Collections.Generic;
using System.IO;
using System.Linq;
using UBot.Core;

namespace UBot.Trade.Components;

internal static class TradeConfig
{
    public static string TracePlayerName
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.TracePlayerName", "");
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.TracePlayerName", value);
    }

    public static bool TracePlayer
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.TracePlayer", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.TracePlayer", value);
    }

    public static bool MountTransport
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.MountTransport", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.MountTransport", value);
    }

    public static bool UseRouteScripts
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.UseRouteScripts", true);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.UseRouteScripts", value);
    }

    public static int SelectedRouteListIndex
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.SelectedRouteListIndex", 0);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.SelectedRouteListIndex", value);
    }

    public static bool RunTownScript
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.RunTownScript", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.RunTownScript", value);
    }

    public static bool WaitForHunter
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.WaitForHunter", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.WaitForHunter", value);
    }

    public static bool AttackThiefPlayers
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.AttackThiefPlayers", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.AttackThiefPlayers", value);
    }

    public static bool AttackThiefNpcs
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.AttackThiefNpcs", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.AttackThiefNpcs", value);
    }

    public static bool CastBuffs
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.CastBuffs", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.CastBuffs", value);
    }

    public static bool CounterAttack
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.CounterAttack", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.CounterAttack", value);
    }

    public static bool ProtectTransport
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.ProtectTransport", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.ProtectTransport", value);
    }

    public static bool BuyGoods
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.BuyGoods", true);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.BuyGoods", value);
    }

    public static bool SellGoods
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.SellGoods", true);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.SellGoods", value);
    }

    public static int BuyGoodsQuantity
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.BuyGoodsQuantity", 0);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.BuyGoodsQuantity", value);
    }

    public static int MaxTransportDistance
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Trade.MaxTransportDistance", 15);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Trade.MaxTransportDistance", value == 0 ? 1 : value);
    }

    public static List<string> RouteScriptList
    {
        get
        {
            var result = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Trade.RouteScriptList", ';').ToList();

            if (!result.Contains("Default"))
                result.Add("Default");

            return result;
        }
        set => UBot.Core.RuntimeAccess.Player.SetArray("UBot.Trade.RouteScriptList", value, ";");
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
                UBot.Core.RuntimeAccess.Player.SetArray($"UBot.Trade.RouteScriptList.{scriptList.Key}", scriptList.Value);

            RouteScriptList = value.Keys.ToList();
        }
    }
}
