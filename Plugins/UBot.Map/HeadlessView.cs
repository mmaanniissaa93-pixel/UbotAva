using System.Windows.Forms;

namespace UBot.Map.Views
{
    internal static class View
    {
        public static Main Instance { get; } = new();
    }

    internal class Main : Panel
    {
        public void InitUniqueObjects()
        {
        }
    }
}
