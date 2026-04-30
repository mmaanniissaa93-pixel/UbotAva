using System.Collections.Generic;
using System.Linq;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Components.Scripting;
using UBot.Core.Objects;

namespace UBot.Trade.Components.Scripting;

internal class BuyGoodsScriptCommand : IScriptCommand
{
    #region Properties

    /// <summary>
    ///     The name of the command.
    /// </summary>
    public string Name => "buy-goods";

    /// <summary>
    ///     A value indicating if the command is busy.
    /// </summary>
    public bool IsBusy { get; private set; }

    /// <summary>
    ///     A dictionary of available arguments for this command.
    /// </summary>
    public Dictionary<string, string> Arguments => new() { { "Codename", "The code name of the NPC" } };

    #endregion Properties

    #region Methods

    /// <summary>
    ///     Executes the command.
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public bool Execute(string[] arguments = null)
    {
        if (arguments == null || arguments.Length < Arguments.Count)
        {
            Log.Warn("[Script] Invalid buy command: NPC code name information missing.");

            return false;
        }

        if (UBot.Core.RuntimeAccess.Session.Player.JobTransport == null)
        {
            Log.Warn("[Script] Can not buy items: No active job transport.");

            return false;
        }

        if (!TradeConfig.SellGoods && !TradeConfig.BuyGoods)
            return true;

        if (IsBusy || ShoppingManager.Running)
            return false;

        try
        {
            IsBusy = true;
            var codeName = arguments[0];

            ShoppingManager.Running = true;
            ShoppingManager.ChooseTalkOption(codeName, TalkOption.Trade);

            if (UBot.Core.RuntimeAccess.Session.SelectedEntity == null)
            {
                Log.Warn("[Script] Can not buy items: Not in dialog with an NPC.");

                return false;
            }

            Log.Notify($"[Script] Selling goods to {UBot.Core.RuntimeAccess.Session.SelectedEntity.Record.GetRealName()}...");

            SellGoods();

            Log.Notify($"[Script] Purchasing goods from {UBot.Core.RuntimeAccess.Session.SelectedEntity.Record.GetRealName()}...");

            BuyGoods();

            ShoppingManager.CloseShop();

            return true;
        }
        finally
        {
            IsBusy = false;
            ShoppingManager.Running = false;
        }
    }

    /// <summary>
    ///     Sells all specialty goods to the selected trader which are
    ///     not in the merchant's inventory.
    /// </summary>
    private void SellGoods()
    {
        if (!TradeConfig.SellGoods || UBot.Core.RuntimeAccess.Session.Player.JobTransport == null)
            return;

        var items = UBot.Core.RuntimeAccess.Session.Player.JobTransport.Inventory.ToArray();
        if (items.Length == 0)
            return;

        var shopGroup = UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefShopGroup(UBot.Core.RuntimeAccess.Session.SelectedEntity?.Record.CodeName);
        if (shopGroup == null)
        {
            Log.Warn("[Script] Can not buy items: Can not find the shop info.");

            return;
        }

        var shopGoods = UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefShopGoods(shopGroup);

        foreach (var item in items)
        {
            var canSellToNpc =
                shopGoods.FirstOrDefault(i =>
                    UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefPackageItem(i.RefPackageItemCodeName).RefItem.ID == item.ItemId
                ) == null;

            if (!canSellToNpc)
                continue;

            ShoppingManager.SellItem(item, UBot.Core.RuntimeAccess.Session.Player.JobTransport.Bionic);
        }
    }

    /// <summary>
    ///     Buys specialty goods from the selected merchant.
    /// </summary>
    private void BuyGoods()
    {
        if (!TradeConfig.BuyGoods)
            return;

        var shopGroup = UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefShopGroup(UBot.Core.RuntimeAccess.Session.SelectedEntity?.Record.CodeName);
        if (shopGroup == null)
        {
            Log.Warn("[Script] Can not buy items: Can not find the shop info.");

            return;
        }

        var shopGoods = UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefShopGoods(shopGroup);
        var item = shopGoods?.FirstOrDefault();

        if (item == null)
        {
            Log.Warn("[Script] Can not buy items: Can not find the shop info.");

            return;
        }

        var tabIndex = UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefShopGoodTabIndex(UBot.Core.RuntimeAccess.Session.SelectedEntity?.Record.CodeName, item);
        if (tabIndex == 0xFF) //Specified item not available in this shop!
        {
            Log.Warn("[Script] Can not buy items: Can not find the item in the shop.");

            return;
        }

        var packageItem = UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefPackageItem(item.RefPackageItemCodeName);
        if (packageItem?.RefItem == null)
        {
            Log.Warn("[Script] Can not buy items: Can not find the referenced item.");

            return;
        }

        var bought = 0;
        var maxSteps = UBot.Core.RuntimeAccess.Session.Player.JobTransport.Inventory.Capacity;
        var existingItemsCount = UBot.Core.RuntimeAccess.Session.Player.JobTransport.Inventory.GetSumAmount(packageItem.RefItemCodeName);
        while (
            !UBot.Core.RuntimeAccess.Session.Player.JobTransport.Inventory.Full
            && (existingItemsCount < TradeConfig.BuyGoodsQuantity || TradeConfig.BuyGoodsQuantity == 0)
        )
        {
            //Avoid endless loop
            if (--maxSteps == 0)
                break;

            var buyNextQty = packageItem.RefItem.MaxStack;
            if (buyNextQty == 0)
                break;

            if (TradeConfig.BuyGoodsQuantity > 0 && bought + buyNextQty > TradeConfig.BuyGoodsQuantity)
                buyNextQty = TradeConfig.BuyGoodsQuantity - bought;

            ShoppingManager.PurchaseItem(UBot.Core.RuntimeAccess.Session.Player.JobTransport, tabIndex, item.SlotIndex, (ushort)buyNextQty);

            bought += buyNextQty;
            existingItemsCount = UBot.Core.RuntimeAccess.Session.Player.JobTransport.Inventory.GetSumAmount(packageItem.RefItemCodeName);
        }
    }

    /// <summary>
    ///     Stops the command.
    /// </summary>
    public void Stop()
    {
        IsBusy = false;
    }

    #endregion Methods
}
