using System.Collections.Generic;
using UBot.Core;

namespace UBot.CommandCenter.Components;

internal class PluginConfig
{
    public static bool Enabled => UBot.Core.RuntimeAccess.Player.Get("UBot.CommandCenter.Enabled", true);

    public static Dictionary<string, string> GetEmoteToCommandMapping()
    {
        var result = new Dictionary<string, string>(16);

        foreach (var emoticon in Emoticons.Items)
        {
            var actionName = UBot.Core.RuntimeAccess.Player.Get(
                $"UBot.CommandCenter.MappedEmotes.{emoticon.Name}",
                Emoticons.GetEmoticonDefaultCommand(emoticon.Name)
            );

            result.Add(emoticon.Name, actionName);
        }

        return result;
    }

    public static string GetAssignedEmoteCommand(string emoteName)
    {
        var mapping = GetEmoteToCommandMapping();

        return mapping.ContainsKey(emoteName) ? mapping[emoteName] : Emoticons.GetEmoticonDefaultCommand(emoteName);
    }
}
