using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Party;

public sealed class PartyMemberRow
{
    public uint MemberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Guild { get; set; } = string.Empty;
    public string HpMp { get; set; } = "-";
    public string Position { get; set; } = "-";
}

public sealed class PartyMatchingRow
{
    public int No { get; set; }
    public uint Id { get; set; }
    public string Race { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int Member { get; set; }
    public string Range { get; set; } = string.Empty;
}

public sealed class PartyBuffSkillRow
{
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsLowLevel { get; set; }

    public override string ToString() => Name;
}

public sealed class PartyMemberBuffRow
{
    public uint Id { get; set; }
    public string Display { get; set; } = string.Empty;

    public override string ToString() => Display;
}

public sealed class PartyBuffAssignmentRow
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public List<uint> Buffs { get; set; } = new();
}

public partial class PartyFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private bool _syncing;
    private string _activeTab = "party";
    private uint _selectedMatchingId;

    private readonly ObservableCollection<PartyMemberRow> _partyMembers = new();
    private readonly ObservableCollection<string> _autoPartyPlayers = new();
    private readonly ObservableCollection<string> _commandPlayers = new();
    private readonly ObservableCollection<PartyMatchingRow> _matchingRows = new();
    private readonly ObservableCollection<PartyBuffSkillRow> _buffCatalogRows = new();
    private readonly ObservableCollection<string> _buffGroups = new();
    private readonly ObservableCollection<string> _buffTargetMembers = new();
    private readonly ObservableCollection<PartyMemberBuffRow> _selectedMemberBuffRows = new();

    private readonly List<PartyBuffAssignmentRow> _buffAssignments = new();
    private readonly Dictionary<uint, string> _buffNameById = new();

    public PartyFeatureView()
    {
        InitializeComponent();

        PartyMembersGrid.ItemsSource = _partyMembers;
        AutoPartyPlayersList.ItemsSource = _autoPartyPlayers;
        CommandPlayersList.ItemsSource = _commandPlayers;
        MatchingGrid.ItemsSource = _matchingRows;
        BuffSkillsList.ItemsSource = _buffCatalogRows;
        BuffGroupsList.ItemsSource = _buffGroups;
        BuffPartyMembersList.ItemsSource = _buffTargetMembers;
        MemberBuffsList.ItemsSource = _selectedMemberBuffRows;
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm;

        MainTabs.ActiveTabId = _activeTab;
        MainTabs.SetTabs(new[]
        {
            ("party", "Party"),
            ("autoParty", "Auto Party"),
            ("partyMatching", "Party Matching"),
            ("partyBuff", "Party Buff")
        });
        MainTabs.TabChanged += MainTabs_TabChanged;

        MatchingPurposeSelect.Options = new List<SelectOption>
        {
            new(-1, "All"),
            new(0, "Hunting"),
            new(1, "Quest"),
            new(2, "Trade Union"),
            new(3, "Thief Union")
        };
        MatchingPurposeSelect.SelectedValue = -1;

        SyncTabVisibility();
        _ = LoadFromConfigAsync();
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        var stateRoot = moduleState;
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("party", out var nestedParty))
            stateRoot = nestedParty;

        if (stateRoot.ValueKind != JsonValueKind.Object)
            return;

        _syncing = true;
        try
        {
            if (stateRoot.TryGetProperty("expAutoShare", out var expAutoShareElement) && TryReadBool(expAutoShareElement, out var expAutoShare))
                ExpAutoShareToggle.IsChecked = expAutoShare;

            if (stateRoot.TryGetProperty("itemAutoShare", out var itemAutoShareElement) && TryReadBool(itemAutoShareElement, out var itemAutoShare))
                ItemAutoShareToggle.IsChecked = itemAutoShare;

            if (stateRoot.TryGetProperty("allowInvitations", out var allowInvitationsElement) && TryReadBool(allowInvitationsElement, out var allowInvitations))
                AllowInvitationsToggle.IsChecked = allowInvitations;

            if (stateRoot.TryGetProperty("leaderName", out var leaderNameElement))
            {
                var leaderName = leaderNameElement.GetString() ?? "Not in a party";
                LeaderText.Text = string.IsNullOrWhiteSpace(leaderName)
                    ? "Leader: Not in a party"
                    : $"Leader: {leaderName}";
            }

            if (stateRoot.TryGetProperty("isInParty", out var inPartyElement) && TryReadBool(inPartyElement, out var isInParty))
                LeavePartyBtn.IsEnabled = isInParty;

            if (stateRoot.TryGetProperty("members", out var membersElement) && membersElement.ValueKind == JsonValueKind.Array)
            {
                var previousSelectedMember = BuffPartyMembersList.SelectedItem?.ToString() ?? string.Empty;
                _partyMembers.Clear();

                foreach (var memberElement in membersElement.EnumerateArray())
                {
                    if (memberElement.ValueKind != JsonValueKind.Object)
                        continue;

                    var row = new PartyMemberRow
                    {
                        MemberId = TryReadUInt(memberElement, "memberId", out var memberId) ? memberId : 0,
                        Name = TryReadString(memberElement, "name", out var name) ? name : string.Empty,
                        Level = TryReadInt(memberElement, "level", out var level) ? level : 0,
                        Guild = TryReadString(memberElement, "guild", out var guild) ? guild : string.Empty,
                        HpMp = TryReadString(memberElement, "hpMp", out var hpMp) ? hpMp : "-",
                        Position = TryReadString(memberElement, "position", out var position) ? position : "-"
                    };

                    _partyMembers.Add(row);
                }

                RefreshBuffTargetMembers(previousSelectedMember);
            }

            if (stateRoot.TryGetProperty("matchingResults", out var matchingResultsElement) && matchingResultsElement.ValueKind == JsonValueKind.Array)
            {
                var oldSelectedId = _selectedMatchingId;
                _matchingRows.Clear();

                foreach (var resultElement in matchingResultsElement.EnumerateArray())
                {
                    if (resultElement.ValueKind != JsonValueKind.Object)
                        continue;

                    var row = new PartyMatchingRow
                    {
                        No = TryReadInt(resultElement, "no", out var no) ? no : _matchingRows.Count + 1,
                        Id = TryReadUInt(resultElement, "id", out var id) ? id : 0,
                        Race = TryReadString(resultElement, "race", out var race) ? race : string.Empty,
                        Name = TryReadString(resultElement, "name", out var memberName) ? memberName : string.Empty,
                        Title = TryReadString(resultElement, "title", out var title) ? title : string.Empty,
                        Purpose = TryReadString(resultElement, "purpose", out var purpose) ? purpose : string.Empty,
                        Member = TryReadInt(resultElement, "member", out var member) ? member : 0,
                        Range = TryReadString(resultElement, "range", out var range) ? range : string.Empty
                    };

                    _matchingRows.Add(row);
                }

                var restored = _matchingRows.FirstOrDefault(row => row.Id == oldSelectedId)
                    ?? _matchingRows.FirstOrDefault();

                _selectedMatchingId = restored?.Id ?? 0;
                MatchingGrid.SelectedItem = restored;
            }

            if (stateRoot.TryGetProperty("buffCatalog", out var buffCatalogElement) && buffCatalogElement.ValueKind == JsonValueKind.Array)
            {
                var selectedBuffSkillId = (BuffSkillsList.SelectedItem as PartyBuffSkillRow)?.Id ?? 0;
                _buffNameById.Clear();
                _buffCatalogRows.Clear();

                foreach (var buffElement in buffCatalogElement.EnumerateArray())
                {
                    if (buffElement.ValueKind != JsonValueKind.Object)
                        continue;

                    var skillId = TryReadUInt(buffElement, "id", out var parsedSkillId) ? parsedSkillId : 0;
                    if (skillId == 0)
                        continue;

                    var name = TryReadString(buffElement, "name", out var parsedName)
                        ? parsedName
                        : $"Skill {skillId}";

                    var row = new PartyBuffSkillRow
                    {
                        Id = skillId,
                        Name = name,
                        IsLowLevel = TryReadBool(buffElement, "isLowLevel", out var isLowLevel) && isLowLevel
                    };

                    _buffCatalogRows.Add(row);
                    _buffNameById[skillId] = name;
                }

                BuffSkillsList.SelectedItem = _buffCatalogRows.FirstOrDefault(row => row.Id == selectedBuffSkillId)
                    ?? _buffCatalogRows.FirstOrDefault();
            }

            if (stateRoot.TryGetProperty("memberBuffs", out var memberBuffsElement) && memberBuffsElement.ValueKind == JsonValueKind.Array)
            {
                _buffAssignments.Clear();

                foreach (var assignmentElement in memberBuffsElement.EnumerateArray())
                {
                    if (assignmentElement.ValueKind != JsonValueKind.Object)
                        continue;

                    var assignment = new PartyBuffAssignmentRow
                    {
                        Name = TryReadString(assignmentElement, "name", out var assignmentName) ? assignmentName : string.Empty,
                        Group = TryReadString(assignmentElement, "group", out var assignmentGroup) ? assignmentGroup : string.Empty,
                        Buffs = new List<uint>()
                    };

                    if (assignmentElement.TryGetProperty("buffs", out var buffsElement) && buffsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var buffIdElement in buffsElement.EnumerateArray())
                        {
                            if (TryReadUInt(buffIdElement, out var buffId))
                                assignment.Buffs.Add(buffId);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(assignment.Name))
                        _buffAssignments.Add(assignment);
                }

                RefreshBuffTargetMembers(BuffPartyMembersList.SelectedItem?.ToString());
                RefreshSelectedMemberBuffRows();
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private async System.Threading.Tasks.Task LoadFromConfigAsync()
    {
        if (_vm == null)
            return;

        await _vm.LoadConfigAsync();

        _syncing = true;
        try
        {
            ExpAutoShareToggle.IsChecked = _vm.BoolCfg("expAutoShare", true);
            ItemAutoShareToggle.IsChecked = _vm.BoolCfg("itemAutoShare", true);
            AllowInvitationsToggle.IsChecked = _vm.BoolCfg("allowInvitations", true);

            AcceptAllInvitationsToggle.IsChecked = _vm.BoolCfg("acceptAllInvitations", false);
            AcceptInvitationsFromListToggle.IsChecked = _vm.BoolCfg("acceptInvitationsFromList", false);
            AutoInviteAllPlayersToggle.IsChecked = _vm.BoolCfg("autoInviteAllPlayers", false);
            AutoInviteAllPlayersFromListToggle.IsChecked = _vm.BoolCfg("autoInviteAllPlayersFromList", false);
            AcceptInviteOnlyTrainingToggle.IsChecked = _vm.BoolCfg("acceptInviteOnlyTrainingPlace", false);
            AcceptIfBotStoppedToggle.IsChecked = _vm.BoolCfg("acceptIfBotStopped", false);
            LeaveIfMasterNotToggle.IsChecked = _vm.BoolCfg("leaveIfMasterNot", false);
            LeaveIfMasterNotNameBox.Text = _vm.TextCfg("leaveIfMasterNotName", string.Empty);
            AlwaysFollowMasterToggle.IsChecked = _vm.BoolCfg("alwaysFollowPartyMaster", false);
            ListenPartyMasterCommandsToggle.IsChecked = _vm.BoolCfg("listenPartyMasterCommands", false);
            ListenCommandsInListToggle.IsChecked = _vm.BoolCfg("listenCommandsInList", false);

            AutoJoinByNameToggle.IsChecked = _vm.BoolCfg("autoJoinByName", false);
            AutoJoinByTitleToggle.IsChecked = _vm.BoolCfg("autoJoinByTitle", false);
            AutoJoinByNameBox.Text = _vm.TextCfg("autoJoinByNameText", string.Empty);
            AutoJoinByTitleBox.Text = _vm.TextCfg("autoJoinByTitleText", string.Empty);

            MatchingNameBox.Text = _vm.TextCfg("matchingQueryName", string.Empty);
            MatchingLevelFromBox.Text = ReadInt(_vm.ObjCfg("matchingQueryLevelFrom"), ReadInt(_vm.ObjCfg("matchingLevelFrom"), 1)).ToString(CultureInfo.InvariantCulture);
            MatchingLevelToBox.Text = ReadInt(_vm.ObjCfg("matchingQueryLevelTo"), ReadInt(_vm.ObjCfg("matchingLevelTo"), 140)).ToString(CultureInfo.InvariantCulture);

            var matchingPurpose = ReadInt(_vm.ObjCfg("matchingQueryPurpose"), -1);
            if (matchingPurpose < -1 || matchingPurpose > 3)
                matchingPurpose = -1;
            MatchingPurposeSelect.SelectedValue = matchingPurpose;

            _autoPartyPlayers.Clear();
            foreach (var name in EnumerateStringRows(_vm.ObjCfg("autoPartyPlayers")))
                _autoPartyPlayers.Add(name);

            _commandPlayers.Clear();
            foreach (var name in EnumerateStringRows(_vm.ObjCfg("commandPlayers")))
                _commandPlayers.Add(name);

            HideLowerLevelSkillsToggle.IsChecked = _vm.BoolCfg("buffHideLowLevelSkills", false);

            _buffGroups.Clear();
            foreach (var group in EnumerateStringRows(_vm.ObjCfg("buffGroups")))
                _buffGroups.Add(group);

            _buffAssignments.Clear();
            foreach (var assignment in EnumerateStringRows(_vm.ObjCfg("buffAssignments")))
            {
                var parsed = ParseAssignment(assignment);
                if (parsed != null)
                    _buffAssignments.Add(parsed);
            }

            _selectedMatchingId = (uint)Math.Max(0, ReadInt(_vm.ObjCfg("matchingSelectedId"), 0));
            SyncMatchingRowsFromConfig();
            RefreshBuffTargetMembers(BuffPartyMembersList.SelectedItem?.ToString());
            RefreshSelectedMemberBuffRows();
        }
        finally
        {
            _syncing = false;
        }
    }

    private void MainTabs_TabChanged(string tabId)
    {
        _activeTab = tabId;
        SyncTabVisibility();
    }

    private void SyncTabVisibility()
    {
        PartyPanel.IsVisible = _activeTab == "party";
        AutoPartyPanel.IsVisible = _activeTab == "autoParty";
        PartyMatchingPanel.IsVisible = _activeTab == "partyMatching";
        PartyBuffPanel.IsVisible = _activeTab == "partyBuff";
    }

    private void PartySetting_Toggled(object? sender, RoutedEventArgs e)
    {
        if (_syncing || _vm == null || sender is not ToggleSwitch toggle || toggle.Tag is not string key)
            return;

        _ = _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            [key] = toggle.IsChecked == true
        });
    }

    private void AutoPartySetting_Toggled(object? sender, RoutedEventArgs e)
    {
        if (_syncing || _vm == null || sender is not ToggleSwitch toggle || toggle.Tag is not string key)
            return;

        _ = _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            [key] = toggle.IsChecked == true
        });
    }

    private void AutoJoinSetting_Toggled(object? sender, RoutedEventArgs e)
    {
        if (_syncing || _vm == null || sender is not ToggleSwitch toggle || toggle.Tag is not string key)
            return;

        _ = _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            [key] = toggle.IsChecked == true
        });
    }

    private void LeaveIfMasterNotNameBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (_syncing || _vm == null)
            return;

        _ = _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["leaveIfMasterNotName"] = (LeaveIfMasterNotNameBox.Text ?? string.Empty).Trim()
        });
    }

    private void AutoJoinByNameBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (_syncing || _vm == null)
            return;

        _ = _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["autoJoinByNameText"] = (AutoJoinByNameBox.Text ?? string.Empty).Trim()
        });
    }

    private void AutoJoinTitleConfirm_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        _ = _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["autoJoinByTitleText"] = (AutoJoinByTitleBox.Text ?? string.Empty).Trim()
        });
    }

    private async void AutoPartyPlayerAdd_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        var value = await PromptForTextAsync("Add Auto Party Player", "Player name", "Character name");
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (AddUnique(_autoPartyPlayers, value.Trim()))
            await PersistAutoPartyPlayersAsync();
    }

    private async void AutoPartyPlayerRemove_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        if (AutoPartyPlayersList.SelectedItem is not string selected)
            return;

        if (!RemoveExact(_autoPartyPlayers, selected))
            return;

        await PersistAutoPartyPlayersAsync();
    }

    private async void CommandPlayerAdd_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        var value = await PromptForTextAsync("Add Command Player", "Player name", "Character name");
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (AddUnique(_commandPlayers, value.Trim()))
            await PersistCommandPlayersAsync();
    }

    private async void CommandPlayerRemove_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        if (CommandPlayersList.SelectedItem is not string selected)
            return;

        if (!RemoveExact(_commandPlayers, selected))
            return;

        await PersistCommandPlayersAsync();
    }

    private async void LeaveParty_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        await _vm.PluginActionAsync("party.leave");
    }

    private async void MatchingSearch_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        await _vm.PluginActionAsync("party.matching.search", BuildMatchingSearchPayload());
    }

    private async void MatchingRefresh_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        await _vm.PluginActionAsync("party.matching.refresh", BuildMatchingSearchPayload());
    }

    private async void MatchingAdd_Click(object? sender, RoutedEventArgs e)
    {
        await OpenMatchingFormAsync("party.matching.create", false);
    }

    private async void MatchingFormParty_Click(object? sender, RoutedEventArgs e)
    {
        await OpenMatchingFormAsync("party.matching.form", true);
    }

    private void MatchingGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncing || _vm == null)
            return;

        if (MatchingGrid.SelectedItem is not PartyMatchingRow selected)
            return;

        _selectedMatchingId = selected.Id;
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["matchingSelectedId"] = _selectedMatchingId
        });
    }

    private async void MatchingJoin_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        if (!TryGetSelectedMatchingId(out var matchingId))
            return;

        await _vm.PluginActionAsync("party.matching.join", new Dictionary<string, object?>
        {
            ["matchingId"] = matchingId
        });
    }

    private async void MatchingDelete_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        if (!TryGetSelectedMatchingId(out var matchingId))
            return;

        await _vm.PluginActionAsync("party.matching.delete", new Dictionary<string, object?>
        {
            ["matchingId"] = matchingId
        });
    }

    private async void HideLowerLevelSkillsToggle_Toggled(object? sender, RoutedEventArgs e)
    {
        if (_syncing || _vm == null)
            return;

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["buffHideLowLevelSkills"] = HideLowerLevelSkillsToggle.IsChecked == true
        });
    }

    private async void BuffGroupCreate_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        var value = await PromptForTextAsync("Create Buff Group", "Group name", "Group name");
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!AddUnique(_buffGroups, value.Trim()))
            return;

        await PersistBuffGroupsAsync();
    }

    private async void BuffGroupRemove_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        if (BuffGroupsList.SelectedItem is not string selectedGroup)
            return;

        if (!RemoveExact(_buffGroups, selectedGroup))
            return;

        foreach (var assignment in _buffAssignments.Where(item => string.Equals(item.Group, selectedGroup, StringComparison.OrdinalIgnoreCase)))
            assignment.Group = string.Empty;

        await PersistBuffGroupsAsync();
        await PersistBuffAssignmentsAsync();
        RefreshSelectedMemberBuffRows();
    }

    private void BuffPartyMembersList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshSelectedMemberBuffRows();
    }

    private async void MemberAddBuff_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        var memberName = GetSelectedBuffTargetMember();
        if (string.IsNullOrWhiteSpace(memberName))
            return;

        if (BuffSkillsList.SelectedItem is not PartyBuffSkillRow selectedSkill)
            return;

        var assignment = GetOrCreateAssignment(memberName);
        if (!assignment.Buffs.Contains(selectedSkill.Id))
            assignment.Buffs.Add(selectedSkill.Id);

        if (string.IsNullOrWhiteSpace(assignment.Group) && BuffGroupsList.SelectedItem is string selectedGroup)
            assignment.Group = selectedGroup;

        await PersistBuffAssignmentsAsync();
        RefreshSelectedMemberBuffRows();
    }

    private async void MemberAssignGroup_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        var memberName = GetSelectedBuffTargetMember();
        if (string.IsNullOrWhiteSpace(memberName))
            return;

        if (BuffGroupsList.SelectedItem is not string selectedGroup)
            return;

        var assignment = GetOrCreateAssignment(memberName);
        assignment.Group = selectedGroup;

        await PersistBuffAssignmentsAsync();
        RefreshSelectedMemberBuffRows();
    }

    private async void MemberClearBuffs_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        var memberName = GetSelectedBuffTargetMember();
        if (string.IsNullOrWhiteSpace(memberName))
            return;

        var assignment = _buffAssignments.FirstOrDefault(item => string.Equals(item.Name, memberName, StringComparison.OrdinalIgnoreCase));
        if (assignment == null)
            return;

        assignment.Buffs.Clear();
        assignment.Group = string.Empty;

        await PersistBuffAssignmentsAsync();
        RefreshSelectedMemberBuffRows();
    }

    private async void MemberRemoveSelectedBuff_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        var memberName = GetSelectedBuffTargetMember();
        if (string.IsNullOrWhiteSpace(memberName))
            return;

        if (MemberBuffsList.SelectedItem is not PartyMemberBuffRow selectedBuff)
            return;

        var assignment = _buffAssignments.FirstOrDefault(item => string.Equals(item.Name, memberName, StringComparison.OrdinalIgnoreCase));
        if (assignment == null)
            return;

        assignment.Buffs.RemoveAll(buffId => buffId == selectedBuff.Id);

        await PersistBuffAssignmentsAsync();
        RefreshSelectedMemberBuffRows();
    }

    private async System.Threading.Tasks.Task OpenMatchingFormAsync(string action, bool includeSelectedId)
    {
        if (_vm == null)
            return;

        var initialModel = BuildPartyFormModel();
        var dialog = new PartyFormWindow(initialModel);
        var owner = TopLevel.GetTopLevel(this) as Window;

        PartyFormDialogResult? result = null;
        if (owner != null)
            result = await dialog.ShowDialog<PartyFormDialogResult?>(owner);

        if (result == null)
            return;

        var payload = new Dictionary<string, object?>
        {
            ["purpose"] = result.Purpose,
            ["levelFrom"] = result.LevelFrom,
            ["levelTo"] = result.LevelTo,
            ["expAutoShare"] = result.ExpAutoShare,
            ["itemAutoShare"] = result.ItemAutoShare,
            ["allowInvitations"] = result.AllowInvitations,
            ["title"] = result.Title,
            ["autoReform"] = result.AutoReform,
            ["autoAccept"] = result.AutoAccept
        };

        if (includeSelectedId && TryGetSelectedMatchingId(out var selectedId))
            payload["matchingId"] = selectedId;

        await _vm.PluginActionAsync(action, payload);
    }

    private PartyFormDialogModel BuildPartyFormModel()
    {
        return new PartyFormDialogModel
        {
            Purpose = Math.Clamp(ReadInt(MatchingPurposeSelect.SelectedValue, 0), 0, 3),
            LevelFrom = Math.Clamp(ParseInt(MatchingLevelFromBox.Text, 1), 1, 140),
            LevelTo = Math.Clamp(ParseInt(MatchingLevelToBox.Text, 140), 1, 140),
            ExpAutoShare = ExpAutoShareToggle.IsChecked == true,
            ItemAutoShare = ItemAutoShareToggle.IsChecked == true,
            AllowInvitations = AllowInvitationsToggle.IsChecked == true,
            Title = (_vm?.TextCfg("matchingTitle", "For opening hunting on the silkroad!") ?? "For opening hunting on the silkroad!").Trim(),
            AutoReform = _vm?.BoolCfg("matchingAutoReform", false) == true,
            AutoAccept = _vm?.BoolCfg("matchingAutoAccept", true) != false
        };
    }

    private Dictionary<string, object?> BuildMatchingSearchPayload()
    {
        var levelFrom = Math.Clamp(ParseInt(MatchingLevelFromBox.Text, 1), 1, 140);
        var levelTo = Math.Clamp(ParseInt(MatchingLevelToBox.Text, 140), 1, 140);
        if (levelFrom > levelTo)
            (levelFrom, levelTo) = (levelTo, levelFrom);

        var purpose = ReadInt(MatchingPurposeSelect.SelectedValue, -1);
        if (purpose < -1 || purpose > 3)
            purpose = -1;

        return new Dictionary<string, object?>
        {
            ["name"] = (MatchingNameBox.Text ?? string.Empty).Trim(),
            ["title"] = (AutoJoinByTitleBox.Text ?? string.Empty).Trim(),
            ["purpose"] = purpose,
            ["levelFrom"] = levelFrom,
            ["levelTo"] = levelTo
        };
    }

    private async System.Threading.Tasks.Task PersistAutoPartyPlayersAsync()
    {
        if (_vm == null)
            return;

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["autoPartyPlayers"] = _autoPartyPlayers.ToList()
        });
    }

    private async System.Threading.Tasks.Task PersistCommandPlayersAsync()
    {
        if (_vm == null)
            return;

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["commandPlayers"] = _commandPlayers.ToList()
        });
    }

    private async System.Threading.Tasks.Task PersistBuffGroupsAsync()
    {
        if (_vm == null)
            return;

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["buffGroups"] = _buffGroups.ToList()
        });
    }

    private async System.Threading.Tasks.Task PersistBuffAssignmentsAsync()
    {
        if (_vm == null)
            return;

        var serialized = _buffAssignments
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(SerializeAssignment)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var skillIds = _buffAssignments
            .SelectMany(item => item.Buffs)
            .Distinct()
            .ToList();

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["buffAssignments"] = serialized,
            ["buffSkillIds"] = skillIds
        });
    }

    private void RefreshBuffTargetMembers(string? preferredSelection = null)
    {
        var currentSelection = preferredSelection ?? BuffPartyMembersList.SelectedItem?.ToString();
        _buffTargetMembers.Clear();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in _partyMembers)
        {
            if (string.IsNullOrWhiteSpace(member.Name) || !seen.Add(member.Name))
                continue;

            _buffTargetMembers.Add(member.Name);
        }

        foreach (var assignment in _buffAssignments)
        {
            if (string.IsNullOrWhiteSpace(assignment.Name) || !seen.Add(assignment.Name))
                continue;

            _buffTargetMembers.Add(assignment.Name);
        }

        if (!string.IsNullOrWhiteSpace(currentSelection))
        {
            var restore = _buffTargetMembers.FirstOrDefault(item => string.Equals(item, currentSelection, StringComparison.OrdinalIgnoreCase));
            if (restore != null)
                BuffPartyMembersList.SelectedItem = restore;
        }

        if (BuffPartyMembersList.SelectedItem == null && _buffTargetMembers.Count > 0)
            BuffPartyMembersList.SelectedItem = _buffTargetMembers[0];
    }

    private void RefreshSelectedMemberBuffRows()
    {
        _selectedMemberBuffRows.Clear();

        var memberName = GetSelectedBuffTargetMember();
        if (string.IsNullOrWhiteSpace(memberName))
            return;

        var assignment = _buffAssignments.FirstOrDefault(item => string.Equals(item.Name, memberName, StringComparison.OrdinalIgnoreCase));
        if (assignment == null)
            return;

        foreach (var buffId in assignment.Buffs.Distinct())
        {
            var buffName = _buffNameById.TryGetValue(buffId, out var knownName)
                ? knownName
                : $"Skill {buffId}";

            _selectedMemberBuffRows.Add(new PartyMemberBuffRow
            {
                Id = buffId,
                Display = $"{buffName} ({buffId})"
            });
        }
    }

    private string GetSelectedBuffTargetMember()
    {
        return BuffPartyMembersList.SelectedItem?.ToString()?.Trim() ?? string.Empty;
    }

    private PartyBuffAssignmentRow GetOrCreateAssignment(string memberName)
    {
        var existing = _buffAssignments.FirstOrDefault(item => string.Equals(item.Name, memberName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        var created = new PartyBuffAssignmentRow
        {
            Name = memberName,
            Group = string.Empty,
            Buffs = new List<uint>()
        };

        _buffAssignments.Add(created);
        return created;
    }

    private bool TryGetSelectedMatchingId(out uint matchingId)
    {
        matchingId = 0;

        if (MatchingGrid.SelectedItem is PartyMatchingRow selected && selected.Id > 0)
        {
            _selectedMatchingId = selected.Id;
            matchingId = selected.Id;
            return true;
        }

        if (_selectedMatchingId > 0)
        {
            matchingId = _selectedMatchingId;
            return true;
        }

        return false;
    }

    private void SyncMatchingRowsFromConfig()
    {
        _matchingRows.Clear();
        if (_vm == null)
            return;

        foreach (var row in EnumerateDictionaryRows(_vm.ObjCfg("matchingResults")))
        {
            var matching = new PartyMatchingRow
            {
                No = ReadInt(GetValue(row, "no"), _matchingRows.Count + 1),
                Id = ReadUInt(GetValue(row, "id"), 0),
                Race = ReadString(GetValue(row, "race"), string.Empty),
                Name = ReadString(GetValue(row, "name"), string.Empty),
                Title = ReadString(GetValue(row, "title"), string.Empty),
                Purpose = ReadString(GetValue(row, "purpose"), string.Empty),
                Member = ReadInt(GetValue(row, "member"), 0),
                Range = ReadString(GetValue(row, "range"), string.Empty)
            };

            _matchingRows.Add(matching);
        }

        var selected = _matchingRows.FirstOrDefault(row => row.Id == _selectedMatchingId)
            ?? _matchingRows.FirstOrDefault();

        _selectedMatchingId = selected?.Id ?? 0;
        MatchingGrid.SelectedItem = selected;
    }

    private async System.Threading.Tasks.Task<string?> PromptForTextAsync(string title, string label, string placeholder)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
            return null;

        var dialog = new TextPromptWindow(title, label, placeholder);
        return await dialog.ShowDialog<string?>(owner);
    }

    private static bool AddUnique(ObservableCollection<string> collection, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (collection.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
            return false;

        collection.Add(value.Trim());
        return true;
    }

    private static bool RemoveExact(ObservableCollection<string> collection, string value)
    {
        var target = collection.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        if (target == null)
            return false;

        collection.Remove(target);
        return true;
    }

    private static IEnumerable<string> EnumerateStringRows(object? raw)
    {
        if (raw == null)
            yield break;

        if (raw is string single)
        {
            if (!string.IsNullOrWhiteSpace(single))
                yield return single.Trim();
            yield break;
        }

        if (raw is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var text = item.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                            yield return text.Trim();
                    }
                }
            }
            yield break;
        }

        if (raw is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var text = item?.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                    yield return text.Trim();
            }
        }
    }

    private static IEnumerable<Dictionary<string, object?>> EnumerateDictionaryRows(object? raw)
    {
        if (raw == null)
            yield break;

        if (raw is JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
                yield break;

            foreach (var entry in element.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                    continue;

                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in entry.EnumerateObject())
                    dict[prop.Name] = ConvertJsonElement(prop.Value);

                yield return dict;
            }

            yield break;
        }

        if (raw is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (TryConvertDictionary(item, out var dict))
                    yield return dict;
            }
        }
    }

    private static bool TryConvertDictionary(object? raw, out Dictionary<string, object?> dict)
    {
        dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (raw == null)
            return false;

        if (raw is IDictionary<string, object?> typed)
        {
            foreach (var pair in typed)
                dict[pair.Key] = pair.Value;
            return true;
        }

        if (raw is IDictionary legacy)
        {
            foreach (DictionaryEntry pair in legacy)
            {
                var key = pair.Key?.ToString();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                dict[key] = pair.Value;
            }

            return dict.Count > 0;
        }

        if (raw is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
                dict[prop.Name] = ConvertJsonElement(prop.Value);

            return true;
        }

        return false;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt64(out var i64) => i64,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.ToString()
        };
    }

    private static PartyBuffAssignmentRow? ParseAssignment(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
            return null;

        var parts = serialized.Split(':');
        if (parts.Length != 3)
            return null;

        var name = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var group = parts[1].Trim();
        var buffs = new List<uint>();

        foreach (var token in parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (uint.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                buffs.Add(parsed);
        }

        return new PartyBuffAssignmentRow
        {
            Name = name,
            Group = group,
            Buffs = buffs.Distinct().ToList()
        };
    }

    private static string SerializeAssignment(PartyBuffAssignmentRow assignment)
    {
        var buffString = string.Join(",", assignment.Buffs.Distinct());
        return $"{assignment.Name}:{assignment.Group}:{buffString}";
    }

    private static object? GetValue(IDictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
            return value;

        var keyMatch = dict.Keys.FirstOrDefault(existing => string.Equals(existing, key, StringComparison.OrdinalIgnoreCase));
        return keyMatch != null ? dict[keyMatch] : null;
    }

    private static int ParseInt(string? text, int fallback)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return fallback;
    }

    private static string ReadString(object? raw, string fallback)
    {
        if (raw == null)
            return fallback;

        if (raw is string text)
            return string.IsNullOrWhiteSpace(text) ? fallback : text;

        if (raw is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }

            return fallback;
        }

        var converted = raw.ToString();
        return string.IsNullOrWhiteSpace(converted) ? fallback : converted;
    }

    private static int ReadInt(object? raw, int fallback)
    {
        if (TryConvertInt(raw, out var value))
            return value;

        return fallback;
    }

    private static uint ReadUInt(object? raw, uint fallback)
    {
        if (TryConvertUInt(raw, out var value))
            return value;

        return fallback;
    }

    private static bool TryConvertInt(object? raw, out int value)
    {
        value = 0;
        if (raw == null)
            return false;

        switch (raw)
        {
            case int intValue:
                value = intValue;
                return true;
            case long longValue when longValue >= int.MinValue && longValue <= int.MaxValue:
                value = (int)longValue;
                return true;
            case uint uintValue when uintValue <= int.MaxValue:
                value = (int)uintValue;
                return true;
            case short shortValue:
                value = shortValue;
                return true;
            case ushort ushortValue:
                value = ushortValue;
                return true;
            case byte byteValue:
                value = byteValue;
                return true;
            case double doubleValue when doubleValue >= int.MinValue && doubleValue <= int.MaxValue:
                value = (int)Math.Round(doubleValue);
                return true;
            case float floatValue when floatValue >= int.MinValue && floatValue <= int.MaxValue:
                value = (int)Math.Round(floatValue);
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var parsed):
                value = parsed;
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedString):
                value = parsedString;
                return true;
        }

        return int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryConvertUInt(object? raw, out uint value)
    {
        value = 0;
        if (raw == null)
            return false;

        switch (raw)
        {
            case uint uintValue:
                value = uintValue;
                return true;
            case int intValue when intValue >= 0:
                value = (uint)intValue;
                return true;
            case long longValue when longValue >= 0 && longValue <= uint.MaxValue:
                value = (uint)longValue;
                return true;
            case ushort ushortValue:
                value = ushortValue;
                return true;
            case short shortValue when shortValue >= 0:
                value = (uint)shortValue;
                return true;
            case byte byteValue:
                value = byteValue;
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetUInt32(out var parsed):
                value = parsed;
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.String && uint.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedString):
                value = parsedString;
                return true;
        }

        return uint.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadBool(JsonElement root, string propertyName, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(propertyName, out var property))
            return false;

        return TryReadBool(property, out value);
    }

    private static bool TryReadBool(JsonElement element, out bool value)
    {
        value = false;
        if (element.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String)
            return bool.TryParse(element.GetString(), out value);

        return false;
    }

    private static bool TryReadInt(JsonElement root, string propertyName, out int value)
    {
        value = 0;
        if (!root.TryGetProperty(propertyName, out var property))
            return false;

        return TryReadInt(property, out value);
    }

    private static bool TryReadInt(JsonElement element, out int value)
    {
        value = 0;

        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out value);

        if (element.ValueKind == JsonValueKind.String)
            return int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        return false;
    }

    private static bool TryReadUInt(JsonElement root, string propertyName, out uint value)
    {
        value = 0;
        if (!root.TryGetProperty(propertyName, out var property))
            return false;

        return TryReadUInt(property, out value);
    }

    private static bool TryReadUInt(JsonElement element, out uint value)
    {
        value = 0;

        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetUInt32(out value);

        if (element.ValueKind == JsonValueKind.String)
            return uint.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        return false;
    }

    private static bool TryReadString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return false;

        value = property.GetString() ?? string.Empty;
        return true;
    }
}
