using System.Windows.Forms;

namespace UBot.Quest.Views
{
    using UBot.Quest.Views.Sidebar;

    internal static class View
    {
        public static Main Main { get; } = new();
        public static QuestSidebarElement SidebarElement { get; set; }
    }

    internal class Main : Panel
    {
    }
}

namespace UBot.Quest.Views.Sidebar
{
    internal class QuestSidebarElement : Panel
    {
        public bool HasQuest(uint questId)
        {
            return false;
        }

        public void AddQuest(uint questId)
        {
        }

        public void RemoveQuest(uint questId)
        {
        }
    }
}
