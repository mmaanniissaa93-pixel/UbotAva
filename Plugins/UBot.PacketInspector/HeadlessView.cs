using System.Windows.Forms;

namespace UBot.PacketInspector.Views
{
    internal static class View
    {
        public static Main Instance { get; } = new();
    }

    internal class Main : Panel
    {
    }
}
