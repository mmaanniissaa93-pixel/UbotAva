using System.Collections.Generic;
using System.IO;
using System.Linq;
using UBot.Core.Client.ReferenceObjects.RegionInfo;
using UBot.Core.Objects;

namespace UBot.Core.Client;

public class RegionInfoManager
{
    private static List<LegacyRegionInfoGroup> LegacyRegionInfo { get; set; } = new(1024);
    private static Dictionary<Region, ModernRegionInfo> ModernRegionInfo { get; set; } = new(1024);

    public static void Load()
    {
        if (UBot.Core.RuntimeAccess.Session.DataPk2 == null)
            return;

        if (!UBot.Core.RuntimeAccess.Session.DataPk2.TryGetFile("regioninfo.txt", out var file))
        {
            Log.Error("Could not load regioninfo.txt!");
            return;
        }

        using var stream = file.OpenRead().GetStream();
        using var reader = new StreamReader(stream);

        //Older sro -> Uses groups
        if (UBot.Core.RuntimeAccess.Session.ClientType < GameClientType.Chinese_Old)
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                while (line != null && line.StartsWith('#'))
                {
                    var groupInfo = new LegacyRegionInfoGroup();
                    groupInfo.Load(new ReferenceParser(line));
                    line = groupInfo.ParseEntries(reader);

                    LegacyRegionInfo.Add(groupInfo);
                }
            }
        }
        else
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var regionInfo = new ModernRegionInfo();
                regionInfo.Load(new ReferenceParser(line));

                ModernRegionInfo.TryAdd(regionInfo.Region, regionInfo);
            }
        }
    }

    public static string GetDungeonName(Region region)
    {
        if (ModernRegionInfo.Count > 0 && ModernRegionInfo.TryGetValue(region, out var modernRegionInfo))
            return modernRegionInfo.RegionType;

        return GetLegacyRegionInfo(region)?.DungeonName;
    }

    private static LegacyRegionInfoGroup GetLegacyRegionInfo(Region region)
    {
        return LegacyRegionInfo.FirstOrDefault(ri => ri.Regions.ContainsKey(region));
    }
}
