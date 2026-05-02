using System.Threading.Tasks;
using UBot.CommandCenter.Components;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Network;

namespace UBot.CommandCenter.Network.Handler;

internal class EmoticonRequest : IPacketHandler
{
    public ushort Opcode => 0x3091;

    public PacketDestination Destination => PacketDestination.Server;

    public void Invoke(Packet packet)
    {
        if (!PluginConfig.Enabled)
            return;

        var type = (EmoticonType)packet.ReadByte();
        var emoticon = Emoticons.GetEmoticonItemByType(type);
        var assignedCommand = PluginConfig.GetAssignedEmoteCommand(emoticon.Name);

Task.Run(() =>
        {
            var success = CommandManager.Execute(assignedCommand);
            if (!success)
            {
                var bot = UBot.Core.RuntimeAccess.Core.Bot;
                var botState = bot == null ? "not initialized" : (bot.Running ? "running" : "not running");
                Log.Debug(
                    $"[Command center] Command [{assignedCommand}] for emoticon [{emoticon.Name}] returned false. Bot state: {botState}. This is a feedback command and does not block the main start flow."
                );
            }
        });
    }
}
