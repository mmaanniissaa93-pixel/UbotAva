using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.Services;

namespace UBot.Core.Components;

public class PickupManager
{
    private static IPickupService _service = new PickupService();

    public static bool RunningPlayerPickup => _service.RunningPlayerPickup;

    public static bool RunningAbilityPetPickup => _service.RunningAbilityPetPickup;

    public static List<(string CodeName, bool PickOnlyChar)> PickupFilter => _service.PickupFilter;

    public static bool PickupGold => ServiceRuntime.PickupSettings?.PickupGold ?? true;

    public static bool PickupRareItems => ServiceRuntime.PickupSettings?.PickupRareItems ?? true;

    public static bool PickupBlueItems => ServiceRuntime.PickupSettings?.PickupBlueItems ?? true;

    public static bool PickupQuestItems => ServiceRuntime.PickupSettings?.PickupQuestItems ?? true;

    public static bool PickupAnyEquips => ServiceRuntime.PickupSettings?.PickupAnyEquips ?? true;

    public static bool PickupEverything => ServiceRuntime.PickupSettings?.PickupEverything ?? true;

    public static bool UseAbilityPet => ServiceRuntime.PickupSettings?.UseAbilityPet ?? true;

    public static bool JustPickMyItems => ServiceRuntime.PickupSettings?.JustPickMyItems ?? false;

    public static void Initialize(IPickupService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        ServiceRuntime.Pickup = _service;
    }

    public static void RunPlayer(object playerPosition, object centerPosition, int radius = 50)
    {
        _service.RunPlayer(playerPosition, centerPosition, radius);
    }

    public static void RunAbilityPet(object centerPosition, int radius = 50)
    {
        _service.RunAbilityPet(centerPosition, radius);
    }

    public static void AddFilter(string codeName, bool pickOnlyChar = false)
    {
        _service.AddFilter(codeName, pickOnlyChar);
    }

    public static void RemoveFilter(string codeName)
    {
        _service.RemoveFilter(codeName);
    }

    public static void LoadFilter()
    {
        _service.LoadFilter();
    }

    public static void SaveFilter()
    {
        _service.SaveFilter();
    }

    public static void Stop()
    {
        _service.Stop();
    }
}

public sealed class PickupService : IPickupService
{
    public bool RunningPlayerPickup { get; private set; }

    public bool RunningAbilityPetPickup { get; private set; }

    public List<(string CodeName, bool PickOnlyChar)> PickupFilter { get; } = new();

    public void RunPlayer(object playerPosition, object centerPosition, int radius = 50)
    {
        var runtime = ServiceRuntime.PickupRuntime;
        if (runtime == null || RunningPlayerPickup)
            return;

        RunningPlayerPickup = true;
        try
        {
            var pickOnlyChar = Settings.UseAbilityPet && runtime.PlayerHasActiveAbilityPet;
            var items = runtime.GetItems(i => Condition(i, centerPosition, radius, pickOnlyChar, pickOnlyChar));
            if (items.Count == 0)
                return;

            foreach (var item in items.OrderBy(item => runtime.Distance(item.SourcePosition, playerPosition)))
            {
                if (!RunningPlayerPickup)
                    return;

                while (runtime.PlayerInAction)
                    Thread.Sleep(50);

                if (item.IsSpecialtyGoodBox && runtime.PlayerSpecialtyBagFull)
                    continue;

                runtime.Pickup(item);
            }
        }
        catch (Exception e)
        {
            ServiceRuntime.Log?.Fatal(e);
        }
        finally
        {
            RunningPlayerPickup = false;
        }
    }

    public async void RunAbilityPet(object centerPosition, int radius = 50)
    {
        var runtime = ServiceRuntime.PickupRuntime;
        if (runtime == null || RunningAbilityPetPickup)
            return;

        RunningAbilityPetPickup = true;

        try
        {
            var items = runtime.GetItems(i => Condition(i, centerPosition, radius, true));
            if (items.Count == 0)
                return;

            var abilityPetPosition = runtime.AbilityPetPosition;
            if (abilityPetPosition == null)
                return;

            foreach (var item in items.OrderBy(item => runtime.Distance(item.SourcePosition, abilityPetPosition)))
            {
                if (!RunningAbilityPetPickup)
                    return;

                if (item.IsSpecialtyGoodBox && runtime.PlayerSpecialtyBagFull)
                    continue;

                await runtime.PickupWithAbilityPetAsync(item);
                await Task.Yield();
            }
        }
        catch (Exception e)
        {
            ServiceRuntime.Log?.Fatal(e);
        }
        finally
        {
            RunningAbilityPetPickup = false;
        }
    }

    public void AddFilter(string codeName, bool pickOnlyChar = false)
    {
        PickupFilter.RemoveAll(p => p.CodeName == codeName);
        PickupFilter.Add((codeName, pickOnlyChar));

        SaveFilter();
    }

    public void RemoveFilter(string codeName)
    {
        PickupFilter.RemoveAll(p => p.CodeName == codeName);
        SaveFilter();
    }

    public void LoadFilter()
    {
        PickupFilter.Clear();

        var config = Settings.LoadPickupFilter();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in config)
        {
            var split = item.Split('|');
            if (split.Length < 2)
                continue;

            if (string.IsNullOrWhiteSpace(split[0]))
                continue;

            if (!bool.TryParse(split[1], out var pickOnlyChar))
                continue;

            var key = $"{split[0]}|{pickOnlyChar}";
            if (!seen.Add(key))
                continue;

            PickupFilter.Add((split[0], pickOnlyChar));
        }
    }

    public void SaveFilter()
    {
        var array = PickupFilter.Select(p => $"{p.CodeName}|{p.PickOnlyChar}").ToArray();
        Settings.SavePickupFilter(array);
    }

    public void Stop()
    {
        RunningPlayerPickup = false;
        RunningAbilityPetPickup = false;
    }

    private static IPickupSettings Settings => ServiceRuntime.PickupSettings ?? DefaultPickupSettings.Instance;

    private bool Condition(
        IPickupItem item,
        object centerPosition,
        int radius,
        bool applyPickOnlyChar = false,
        bool pickOnlyChar = false
    )
    {
        var runtime = ServiceRuntime.PickupRuntime;
        if (runtime == null)
            return false;

        var playerJid = runtime.PlayerJid;

        if (Settings.JustPickMyItems && item.OwnerJid != playerJid)
            return false;

        const int tolerance = 15;
        if (runtime.Distance(item.SourcePosition, centerPosition) > radius + tolerance)
            return false;

        if (applyPickOnlyChar && item.IsBehindObstacle)
            return false;

        var isItemAutoShareParty = runtime.IsItemAutoShareParty;
        if (isItemAutoShareParty && Settings.PickupGold && item.IsGold)
        {
            if (!(applyPickOnlyChar && pickOnlyChar))
                return true;
        }

        if (item.HasOwner && item.OwnerJid != playerJid)
        {
            if (!isItemAutoShareParty)
                return false;

            if (item.IsQuest && runtime.IsPartyMember(item.OwnerJid))
                return false;
        }

        if (Settings.PickupGold && item.IsGold && !(applyPickOnlyChar && pickOnlyChar))
            return true;

        if (
            (Settings.PickupRareItems && item.Rarity >= 2)
            || (Settings.PickupBlueItems && item.Rarity >= 1)
            || (Settings.PickupAnyEquips && item.IsEquip)
            || (Settings.PickupQuestItems && item.IsQuest)
            || Settings.PickupEverything
        )
            return true;

        return applyPickOnlyChar
            ? PickupFilter.Any(p => p.CodeName == item.CodeName && p.PickOnlyChar == pickOnlyChar)
            : PickupFilter.Any(p => p.CodeName == item.CodeName);
    }
}

internal sealed class DefaultPickupSettings : IPickupSettings
{
    public static readonly DefaultPickupSettings Instance = new();

    public bool PickupGold => true;
    public bool PickupRareItems => true;
    public bool PickupBlueItems => true;
    public bool PickupQuestItems => true;
    public bool PickupAnyEquips => true;
    public bool PickupEverything => true;
    public bool UseAbilityPet => true;
    public bool JustPickMyItems => false;

    public string[] LoadPickupFilter() => Array.Empty<string>();

    public void SavePickupFilter(string[] values) { }
}
