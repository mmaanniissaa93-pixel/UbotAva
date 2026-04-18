using System.Windows.Forms;

namespace UBot.AutoDungeon.Views
{
    internal static class View
    {
        public static AutoDungeonView Instance { get; } = new();
    }

    internal class AutoDungeonView : Panel
    {
        public void ReloadFromState()
        {
        }
    }
}
