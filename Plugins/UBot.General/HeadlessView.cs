using System.Windows.Forms;
using UBot.Core.Network;

namespace UBot.General.Views
{
    internal static class View
    {
        public static Main Instance { get; } = new();
        public static PendingWindow PendingWindow { get; } = new();
        public static AccountsWindow AccountsWindow { get; } = new();
    }

    internal class Main : Panel
    {
    }

    internal class PendingWindow : Form
    {
        public void Start(int count, int timestamp)
        {
        }

        public void Update(Packet packet)
        {
        }

        public void StopClientlessQueueTask()
        {
        }

        public void ShowAtTop(Control owner)
        {
        }
    }

    internal class AccountsWindow : Form
    {
    }
}
