using System.Windows.Forms;
using UBot.Core.Objects;

namespace UBot.Alchemy.Views
{
    internal class Main : Panel
    {
        public bool IsRefreshing { get; set; }

        public InventoryItem SelectedItem { get; set; }

        public void AddLog(string itemName, string message)
        {
            if (!string.IsNullOrEmpty(message))
                UBot.Core.Log.Debug($"[Alchemy][Headless] {itemName}: {message}");
        }
    }
}
