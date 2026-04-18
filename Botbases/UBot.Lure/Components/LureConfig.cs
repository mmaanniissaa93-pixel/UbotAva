using UBot.Core;
using UBot.Core.Client.ReferenceObjects;
using UBot.Core.Objects;

namespace UBot.Lure.Components;

internal static class LureConfig
{
    public static bool UseSpeedDrug
    {
        get => PlayerConfig.Get("UBot.Training.checkUseSpeedDrug", true);
        set => PlayerConfig.Set("UBot.Training.checkUseSpeedDrug", value);
    }

    public static bool UseHowlingShout
    {
        get => PlayerConfig.Get("UBot.Lure.UseHowlingShout", false) && Game.Player.Race == ObjectCountry.Europe;
        set => PlayerConfig.Set("UBot.Lure.UseHowlingShout", value);
    }

    public static bool UseNormalAttack
    {
        get => PlayerConfig.Get("UBot.Lure.UseNormalAttack", false);
        set => PlayerConfig.Set("UBot.Lure.UseNormalAttack", value);
    }

    public static bool StopIfNumPartyMemberDead
    {
        get => PlayerConfig.Get("UBot.Lure.StopIfNumPartyMemberDead", false);
        set => PlayerConfig.Set("UBot.Lure.StopIfNumPartyMemberDead", value);
    }

    public static int NumPartyMemberDead
    {
        get => PlayerConfig.Get("UBot.Lure.NumPartyMemberDead", 3);
        set => PlayerConfig.Set("UBot.Lure.NumPartyMemberDead", value);
    }

    public static bool StopIfNumPartyMember
    {
        get => PlayerConfig.Get("UBot.Lure.StopIfNumPartyMember", false);
        set => PlayerConfig.Set("UBot.Lure.StopIfNumPartyMember", value);
    }

    public static int NumPartyMember
    {
        get => PlayerConfig.Get("UBot.Lure.NumPartyMember", 3);
        set => PlayerConfig.Set("UBot.Lure.NumPartyMember", value);
    }

    public static bool StopIfNumMonsterType
    {
        get => PlayerConfig.Get("UBot.Lure.StopIfNumMonsterType", false);
        set => PlayerConfig.Set("UBot.Lure.StopIfNumMonsterType", value);
    }

    public static MonsterRarity SelectedMonsterType
    {
        get => PlayerConfig.GetEnum("UBot.Lure.SelectedMonsterType", MonsterRarity.General);
        set => PlayerConfig.Set("UBot.Lure.SelectedMonsterType", (byte)value);
    }

    public static int NumMonsterType
    {
        get => PlayerConfig.Get("UBot.Lure.NumMonsterType", 3);
        set => PlayerConfig.Set("UBot.Lure.NumMonsterType", value);
    }

    public static bool StopIfNumPartyMembersOnSpot
    {
        get => PlayerConfig.Get("UBot.Lure.StopIfNumPartyMembersOnSpot", false);
        set => PlayerConfig.Set("UBot.Lure.StopIfNumPartyMembersOnSpot", value);
    }

    public static int NumPartyMembersOnSpot
    {
        get => PlayerConfig.Get("UBot.Lure.NumPartyMembersOnSpot", 3);
        set => PlayerConfig.Set("UBot.Lure.NumPartyMembersOnSpot", value);
    }

    public static bool UseScript
    {
        get => PlayerConfig.Get("UBot.Lure.UseScript", false);
        set => PlayerConfig.Set("UBot.Lure.UseScript", value);
    }

    public static string SelectedScriptPath
    {
        get => PlayerConfig.Get("UBot.Lure.SelectedScriptPath", "");
        set => PlayerConfig.Set("UBot.Lure.SelectedScriptPath", value);
    }

    public static bool WalkRandomly
    {
        get => PlayerConfig.Get("UBot.Lure.WalkRandomly", true);
        set => PlayerConfig.Set("UBot.Lure.WalkRandomly", value);
    }

    public static bool StayAtCenter
    {
        get => PlayerConfig.Get("UBot.Lure.StayAtCenter", false);
        set => PlayerConfig.Set("UBot.Lure.StayAtCenter", value);
    }

    public static bool StayAtCenterFor
    {
        get => PlayerConfig.Get("UBot.Lure.StayAtCenterFor", false);
        set => PlayerConfig.Set("UBot.Lure.StayAtCenterFor", value);
    }

    public static int StayAtCenterForSeconds
    {
        get => PlayerConfig.Get("UBot.Lure.StayAtCenterForSeconds", 5);
        set => PlayerConfig.Set("UBot.Lure.StayAtCenterForSeconds", value);
    }

    public static bool NoHowlingAtCenter
    {
        get => PlayerConfig.Get("UBot.Lure.NoHowlingAtCenter", true);
        set => PlayerConfig.Set("UBot.Lure.NoHowlingAtCenter", value);
    }

    public static bool UseAttackingSkills
    {
        get => PlayerConfig.Get("UBot.Lure.UseAttackingSkills", false);
        set => PlayerConfig.Set("UBot.Lure.UseAttackingSkills", value);
    }

    public static string WalkscriptPath
    {
        get => PlayerConfig.Get("UBot.Lure.Walkback.File", "");
        set => PlayerConfig.Set("UBot.Lure.Walkback.File", value);
    }

    public static Area Area
    {
        get
        {
            var region = PlayerConfig.Get<ushort>("UBot.Lure.Area.Region");
            var x = PlayerConfig.Get("UBot.Lure.Area.X", 0f);
            var y = PlayerConfig.Get("UBot.Lure.Area.Y", 0f);
            var z = PlayerConfig.Get("UBot.Lure.Area.Z", 0f);
            var r = PlayerConfig.Get("UBot.Lure.Area.Radius", 50);

            return new Area
            {
                Name = "Lure",
                Position = new Position(region, x, y, z),
                Radius = r,
            };
        }
        set
        {
            PlayerConfig.Set("UBot.Lure.Area.Region", value.Position.Region);
            PlayerConfig.Set("UBot.Lure.Area.X", value.Position.XOffset);
            PlayerConfig.Set("UBot.Lure.Area.Y", value.Position.YOffset);
            PlayerConfig.Set("UBot.Lure.Area.Z", value.Position.ZOffset);
            PlayerConfig.Set("UBot.Lure.Area.Radius", value.Radius);
        }
    }
}
