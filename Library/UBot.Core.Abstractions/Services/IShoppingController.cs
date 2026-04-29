namespace UBot.Core.Abstractions.Services;

public interface IShoppingController
{
    bool Running { get; }
    void CloseShop();
    void CloseGuildShop();
}
