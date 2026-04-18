using System.Windows.Forms;

namespace UBot.TargetAssist.Views
{
    internal static class View
    {
        public static Main Instance { get; } = new();
    }

    internal class Main : Panel
    {
    }
}
