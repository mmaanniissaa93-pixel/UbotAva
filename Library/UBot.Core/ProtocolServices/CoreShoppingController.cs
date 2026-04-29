using UBot.Core.Abstractions.Services;
using UBot.Core.Components;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreShoppingController : IShoppingController
{
    public bool Running => ShoppingManager.Running;

    public void CloseShop() => ShoppingManager.CloseShop();

    public void CloseGuildShop() => ShoppingManager.CloseGuildShop();
}

