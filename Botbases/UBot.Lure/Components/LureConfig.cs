using UBot.Core;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Objects;

namespace UBot.Lure.Components;

internal static class LureConfig
{
    public static bool UseSpeedDrug
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkUseSpeedDrug", true);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Training.checkUseSpeedDrug", value);
    }

    public static bool UseHowlingShout
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.UseHowlingShout", false) && UBot.Core.RuntimeAccess.Session.Player.Race == ObjectCountry.Europe;
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.UseHowlingShout", value);
    }

    public static bool UseNormalAttack
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.UseNormalAttack", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.UseNormalAttack", value);
    }

    public static bool StopIfNumPartyMemberDead
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StopIfNumPartyMemberDead", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.StopIfNumPartyMemberDead", value);
    }

    public static int NumPartyMemberDead
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NumPartyMemberDead", 3);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.NumPartyMemberDead", value);
    }

    public static bool StopIfNumPartyMember
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StopIfNumPartyMember", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.StopIfNumPartyMember", value);
    }

    public static int NumPartyMember
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NumPartyMember", 3);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.NumPartyMember", value);
    }

    public static bool StopIfNumMonsterType
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StopIfNumMonsterType", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.StopIfNumMonsterType", value);
    }

    public static MonsterRarity SelectedMonsterType
    {
        get => UBot.Core.RuntimeAccess.Player.GetEnum("UBot.Lure.SelectedMonsterType", MonsterRarity.General);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.SelectedMonsterType", (byte)value);
    }

    public static int NumMonsterType
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NumMonsterType", 3);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.NumMonsterType", value);
    }

    public static bool StopIfNumPartyMembersOnSpot
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StopIfNumPartyMembersOnSpot", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.StopIfNumPartyMembersOnSpot", value);
    }

    public static int NumPartyMembersOnSpot
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NumPartyMembersOnSpot", 3);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.NumPartyMembersOnSpot", value);
    }

    public static bool UseScript
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.UseScript", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.UseScript", value);
    }

    public static string SelectedScriptPath
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.SelectedScriptPath", "");
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.SelectedScriptPath", value);
    }

    public static bool WalkRandomly
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.WalkRandomly", true);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.WalkRandomly", value);
    }

    public static bool StayAtCenter
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StayAtCenter", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.StayAtCenter", value);
    }

    public static bool StayAtCenterFor
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StayAtCenterFor", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.StayAtCenterFor", value);
    }

    public static int StayAtCenterForSeconds
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StayAtCenterForSeconds", 5);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.StayAtCenterForSeconds", value);
    }

    public static bool NoHowlingAtCenter
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NoHowlingAtCenter", true);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.NoHowlingAtCenter", value);
    }

    public static bool UseAttackingSkills
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.UseAttackingSkills", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.UseAttackingSkills", value);
    }

    public static string WalkscriptPath
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Walkback.File", "");
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.Walkback.File", value);
    }

    public static Area Area
    {
        get
        {
            var region = UBot.Core.RuntimeAccess.Player.Get<ushort>("UBot.Lure.Area.Region");
            var x = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Area.X", 0f);
            var y = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Area.Y", 0f);
            var z = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Area.Z", 0f);
            var r = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Area.Radius", 50);

            return new Area
            {
                Name = "Lure",
                Position = new Position(region, x, y, z),
                Radius = r,
            };
        }
        set
        {
            UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.Area.Region", value.Position.Region);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.Area.X", value.Position.XOffset);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.Area.Y", value.Position.YOffset);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.Area.Z", value.Position.ZOffset);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.Area.Radius", value.Radius);
        }
    }
}
