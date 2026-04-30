using System.Windows.Forms;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Chat.Views
{
    internal static class View
    {
        public static Main Instance { get; } = new();
    }

    internal class Main : Panel
    {
        public UniqueTextSink UniqueText { get; } = new();

        public void AppendMessage(string message, string sender, ChatType type)
        {
            UBot.Core.RuntimeAccess.Events.FireEvent(
                "OnChatMessage",
                sender ?? string.Empty,
                message ?? string.Empty,
                type);
        }
    }

    internal sealed class UniqueTextSink
    {
        public void Write(string message)
        {
            UBot.Core.RuntimeAccess.Events.FireEvent("OnUniqueMessage", message ?? string.Empty);
        }
    }
}
