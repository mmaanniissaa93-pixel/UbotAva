using System.Windows.Forms;

namespace UBot.Party.Views
{
    internal static class View
    {
        public static Main Instance { get; } = new();
        public static PartyWindow PartyWindow { get; } = new();
    }

    internal class Main : Panel
    {
    }

    internal class PartyWindow : Form
    {
    }
}
