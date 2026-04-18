using System.Windows.Forms;

namespace UBot.Skills.Views
{
    internal static class View
    {
        public static Main Instance { get; } = new();
    }

    internal class Main : Panel
    {
    }
}
