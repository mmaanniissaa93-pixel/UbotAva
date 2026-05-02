using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Event;
using Forms = System.Windows.Forms;

namespace UBot.Avalonia.Features.Lure;

public sealed class LureRecorderWindow : Window
{
    private sealed class LureRecorderCommandEntry
    {
        public string Type { get; set; } = "move";
        public Dictionary<string, string> Params { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static readonly string[] CommandTypes =
    {
        "buy",
        "move",
        "repair",
        "cast",
        "store",
        "supply",
        "teleport",
        "wait"
    };

    private static readonly object GlobalSync = new();
    private static bool _eventsSubscribed;
    private static WeakReference<LureRecorderWindow>? _activeRecorder;
    private static Action? _playerMoveHandler;
    private static Action<uint>? _skillCastHandler;

    private readonly Func<string, Task>? _onScriptSaved;
    private readonly List<LureRecorderCommandEntry> _commands = new();
    private readonly ObservableCollection<string> _commandLines = new();
    private readonly string _initialPath;

    private readonly ComboBox _commandTypeCombo;
    private readonly TextBox _codenameBox;
    private readonly TextBox _destinationBox;
    private readonly TextBox _timeBox;
    private readonly TextBox _xOffsetBox;
    private readonly TextBox _yOffsetBox;
    private readonly TextBox _zOffsetBox;
    private readonly TextBox _xSectorBox;
    private readonly TextBox _ySectorBox;
    private readonly ListBox _commandsList;
    private readonly TextBlock _statusLabel;

    private readonly Control _codenameField;
    private readonly Control _destinationField;
    private readonly Control _timeField;
    private readonly Control _xOffsetField;
    private readonly Control _yOffsetField;
    private readonly Control _zOffsetField;
    private readonly Control _xSectorField;
    private readonly Control _ySectorField;

    private bool _isRecording;
    private string _scriptPath;
    private string _lastMoveSignature = string.Empty;
    private string _lastCastSignature = string.Empty;
    private long _lastCastAtMs;

    public LureRecorderWindow(string scriptPath, Func<string, Task>? onScriptSaved)
    {
        _scriptPath = scriptPath?.Trim() ?? string.Empty;
        _initialPath = _scriptPath;
        _onScriptSaved = onScriptSaved;

        Title = "UBot - Lure Script Recorder";
        Width = 980;
        Height = 700;
        MinWidth = 840;
        MinHeight = 600;

        EnsureGlobalEventSubscriptions();

        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Margin = new Thickness(12)
        };

        var topToolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var startBtn = new Button { Content = "Start", Width = 80 };
        startBtn.Click += (_, _) =>
        {
            _isRecording = true;
            _lastMoveSignature = string.Empty;
            _lastCastSignature = string.Empty;
            _lastCastAtMs = 0;
            SetStatus("Recording");
        };

        var stopBtn = new Button { Content = "Stop", Width = 80 };
        stopBtn.Click += (_, _) =>
        {
            _isRecording = false;
            SetStatus("Stopped");
        };

        var clearBtn = new Button { Content = "Clear", Width = 80 };
        clearBtn.Click += (_, _) =>
        {
            _commands.Clear();
            RefreshCommandLines();
            SetStatus("Cleared");
        };

        var loadBtn = new Button { Content = "Load", Width = 80 };
        loadBtn.Click += async (_, _) => await PickAndLoadScriptAsync();

        var saveBtn = new Button { Content = "Save", Width = 80 };
        saveBtn.Click += async (_, _) => await SaveScriptAsync(allowSaveAs: false);

        var saveAsBtn = new Button { Content = "Save As", Width = 90 };
        saveAsBtn.Click += async (_, _) => await SaveScriptAsync(allowSaveAs: true);

        var closeBtn = new Button { Content = "Close", Width = 80 };
        closeBtn.Click += (_, _) => Close();

        _statusLabel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Text = "Stopped"
        };

        topToolbar.Children.Add(startBtn);
        topToolbar.Children.Add(stopBtn);
        topToolbar.Children.Add(clearBtn);
        topToolbar.Children.Add(loadBtn);
        topToolbar.Children.Add(saveBtn);
        topToolbar.Children.Add(saveAsBtn);
        topToolbar.Children.Add(closeBtn);
        topToolbar.Children.Add(_statusLabel);

        Grid.SetRow(topToolbar, 0);
        root.Children.Add(topToolbar);

        var mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1.35*,1*"),
            Margin = new Thickness(0, 10, 0, 10)
        };

        _commandsList = new ListBox
        {
            ItemsSource = _commandLines
        };
        _commandsList.SelectionChanged += CommandsList_SelectionChanged;
        Grid.SetColumn(_commandsList, 0);
        mainGrid.Children.Add(_commandsList);

        var rightPanel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(12, 0, 0, 0)
        };

        var commandTypeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        commandTypeRow.Children.Add(new TextBlock { Text = "Command", Width = 120, VerticalAlignment = VerticalAlignment.Center });
        _commandTypeCombo = new ComboBox
        {
            Width = 220,
            ItemsSource = CommandTypes,
            SelectedItem = "move"
        };
        _commandTypeCombo.SelectionChanged += (_, _) => ApplyEditorVisibility();
        commandTypeRow.Children.Add(_commandTypeCombo);
        rightPanel.Children.Add(commandTypeRow);

        _codenameBox = new TextBox { Width = 260 };
        _destinationBox = new TextBox { Width = 260 };
        _timeBox = new TextBox { Width = 260, Text = "1" };
        _xOffsetBox = new TextBox { Width = 260, Text = "0" };
        _yOffsetBox = new TextBox { Width = 260, Text = "0" };
        _zOffsetBox = new TextBox { Width = 260, Text = "0" };
        _xSectorBox = new TextBox { Width = 260, Text = "0" };
        _ySectorBox = new TextBox { Width = 260, Text = "0" };

        _codenameField = CreateField("Codename", _codenameBox);
        _destinationField = CreateField("Destination", _destinationBox);
        _timeField = CreateField("Time (sec)", _timeBox);
        _xOffsetField = CreateField("X Offset", _xOffsetBox);
        _yOffsetField = CreateField("Y Offset", _yOffsetBox);
        _zOffsetField = CreateField("Z Offset", _zOffsetBox);
        _xSectorField = CreateField("X Sector", _xSectorBox);
        _ySectorField = CreateField("Y Sector", _ySectorBox);

        rightPanel.Children.Add(_codenameField);
        rightPanel.Children.Add(_destinationField);
        rightPanel.Children.Add(_timeField);
        rightPanel.Children.Add(_xOffsetField);
        rightPanel.Children.Add(_yOffsetField);
        rightPanel.Children.Add(_zOffsetField);
        rightPanel.Children.Add(_xSectorField);
        rightPanel.Children.Add(_ySectorField);

        var editorButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var addBtn = new Button { Content = "Add", Width = 90 };
        addBtn.Click += (_, _) => AddCommandFromEditor();

        var updateBtn = new Button { Content = "Update Selected", Width = 130 };
        updateBtn.Click += (_, _) => UpdateSelectedFromEditor();

        var removeBtn = new Button { Content = "Remove Selected", Width = 130 };
        removeBtn.Click += (_, _) => RemoveSelectedCommand();

        editorButtons.Children.Add(addBtn);
        editorButtons.Children.Add(updateBtn);
        editorButtons.Children.Add(removeBtn);
        rightPanel.Children.Add(editorButtons);

        Grid.SetColumn(rightPanel, 1);
        mainGrid.Children.Add(rightPanel);

        Grid.SetRow(mainGrid, 1);
        root.Children.Add(mainGrid);

        var footer = new TextBlock
        {
            Text = "Script format: dismount + buy/move/repair/cast/store/supply/teleport/wait",
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        Content = root;

        Opened += async (_, _) =>
        {
            _activeRecorder = new WeakReference<LureRecorderWindow>(this);
            await LoadScriptAsync(string.IsNullOrWhiteSpace(_scriptPath) ? _initialPath : _scriptPath);
            ApplyEditorVisibility();
            SetStatus("Stopped");
        };

        Closed += (_, _) =>
        {
            _isRecording = false;
            if (_activeRecorder != null && _activeRecorder.TryGetTarget(out var current) && ReferenceEquals(current, this))
                _activeRecorder = null;

            TryCleanupEventSubscriptions();
        };
    }

    private static void TryCleanupEventSubscriptions()
    {
        lock (GlobalSync)
        {
            if (_activeRecorder != null && _activeRecorder.TryGetTarget(out var active) && active != null)
                return;

            _eventsSubscribed = false;
        }
    }

    private static Control CreateField(string label, Control input)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        row.Children.Add(new TextBlock
        {
            Text = label,
            Width = 120,
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(input);
        return row;
    }

    private void ApplyEditorVisibility()
    {
        var type = (_commandTypeCombo.SelectedItem?.ToString() ?? "move").Trim().ToLowerInvariant();

        var codenameVisible = type is "buy" or "repair" or "cast" or "store" or "supply" or "teleport";
        var destinationVisible = type == "teleport";
        var timeVisible = type == "wait";
        var moveVisible = type == "move";

        _codenameField.IsVisible = codenameVisible;
        _destinationField.IsVisible = destinationVisible;
        _timeField.IsVisible = timeVisible;
        _xOffsetField.IsVisible = moveVisible;
        _yOffsetField.IsVisible = moveVisible;
        _zOffsetField.IsVisible = moveVisible;
        _xSectorField.IsVisible = moveVisible;
        _ySectorField.IsVisible = moveVisible;
    }

    private void CommandsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedIndex = _commandsList.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _commands.Count)
            return;

        var selected = _commands[selectedIndex];
        _commandTypeCombo.SelectedItem = selected.Type;

        string Value(string key, string fallback = "")
            => selected.Params.TryGetValue(key, out var value) ? value : fallback;

        _codenameBox.Text = Value("codename");
        _destinationBox.Text = Value("destination");
        _timeBox.Text = Value("time", "1");
        _xOffsetBox.Text = Value("xOffset", "0");
        _yOffsetBox.Text = Value("yOffset", "0");
        _zOffsetBox.Text = Value("zOffset", "0");
        _xSectorBox.Text = Value("xSector", "0");
        _ySectorBox.Text = Value("ySector", "0");
        ApplyEditorVisibility();
    }

    private void AddCommandFromEditor()
    {
        if (!TryBuildEditorCommand(out var command, out var error))
        {
            SetStatus(error);
            return;
        }

        _commands.Add(command);
        RefreshCommandLines();
        _commandsList.SelectedIndex = _commands.Count - 1;
        SetStatus("Command added");
    }

    private void UpdateSelectedFromEditor()
    {
        var selectedIndex = _commandsList.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _commands.Count)
        {
            SetStatus("No selected command");
            return;
        }

        if (!TryBuildEditorCommand(out var command, out var error))
        {
            SetStatus(error);
            return;
        }

        _commands[selectedIndex] = command;
        RefreshCommandLines();
        _commandsList.SelectedIndex = selectedIndex;
        SetStatus("Command updated");
    }

    private void RemoveSelectedCommand()
    {
        var selectedIndex = _commandsList.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _commands.Count)
            return;

        _commands.RemoveAt(selectedIndex);
        RefreshCommandLines();
        _commandsList.SelectedIndex = Math.Min(selectedIndex, _commands.Count - 1);
        SetStatus("Command removed");
    }

    private bool TryBuildEditorCommand(out LureRecorderCommandEntry command, out string error)
    {
        command = new LureRecorderCommandEntry();
        error = string.Empty;

        var type = (_commandTypeCombo.SelectedItem?.ToString() ?? "move").Trim().ToLowerInvariant();
        if (!CommandTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            error = "Invalid command type";
            return false;
        }

        command.Type = type;
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string ReadToken(TextBox box)
        {
            var value = box.Text?.Trim() ?? string.Empty;
            return value.Replace(" ", string.Empty);
        }

        if (type is "buy" or "repair" or "cast" or "store" or "supply")
        {
            var codename = ReadToken(_codenameBox);
            if (string.IsNullOrWhiteSpace(codename))
            {
                error = "Codename is required";
                return false;
            }

            parameters["codename"] = codename;
        }
        else if (type == "teleport")
        {
            var codename = ReadToken(_codenameBox);
            var destination = ReadToken(_destinationBox);
            if (string.IsNullOrWhiteSpace(codename))
            {
                error = "Codename is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                error = "Destination is required";
                return false;
            }

            parameters["codename"] = codename;
            parameters["destination"] = destination;
        }
        else if (type == "wait")
        {
            var time = ReadToken(_timeBox);
            if (!int.TryParse(time, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) || seconds < 0)
            {
                error = "Time must be a non-negative integer";
                return false;
            }

            parameters["time"] = seconds.ToString(CultureInfo.InvariantCulture);
        }
        else if (type == "move")
        {
            var xOffset = ReadToken(_xOffsetBox);
            var yOffset = ReadToken(_yOffsetBox);
            var zOffset = ReadToken(_zOffsetBox);
            var xSector = ReadToken(_xSectorBox);
            var ySector = ReadToken(_ySectorBox);

            if (!IsNumeric(xOffset) || !IsNumeric(yOffset) || !IsNumeric(zOffset) || !IsNumeric(xSector) || !IsNumeric(ySector))
            {
                error = "Move parameters must be numeric";
                return false;
            }

            parameters["xOffset"] = xOffset;
            parameters["yOffset"] = yOffset;
            parameters["zOffset"] = zOffset;
            parameters["xSector"] = xSector;
            parameters["ySector"] = ySector;
        }

        command.Params = parameters;
        return true;
    }

    private static bool IsNumeric(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
               || double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out _);
    }

    private void RefreshCommandLines()
    {
        _commandLines.Clear();
        for (var i = 0; i < _commands.Count; i++)
        {
            _commandLines.Add($"{i + 1}. {FormatCommandLine(_commands[i])}");
        }
    }

    private static string FormatCommandLine(LureRecorderCommandEntry command)
    {
        static string Value(Dictionary<string, string> parameters, string key, string fallback = "")
            => parameters.TryGetValue(key, out var value) ? value.Trim() : fallback;

        return command.Type switch
        {
            "dismount" => "dismount",
            "buy" or "repair" or "cast" or "store" or "supply"
                => $"{command.Type} {Value(command.Params, "codename", "NPC_CODENAME")}",
            "teleport"
                => $"teleport {Value(command.Params, "codename", "NPC_CODENAME")} {Value(command.Params, "destination", "0")}",
            "wait"
                => $"wait {Value(command.Params, "time", "1")}",
            "move"
                => $"move {Value(command.Params, "xOffset", "0")} {Value(command.Params, "yOffset", "0")} {Value(command.Params, "zOffset", "0")} {Value(command.Params, "xSector", "0")} {Value(command.Params, "ySector", "0")}",
            _ => command.Type
        };
    }

    private async Task LoadScriptAsync(string path)
    {
        var trimmed = path?.Trim() ?? string.Empty;
        _scriptPath = trimmed;
        _commands.Clear();

        if (!string.IsNullOrWhiteSpace(trimmed) && File.Exists(trimmed))
        {
            try
            {
                var content = await File.ReadAllTextAsync(trimmed);
                _commands.AddRange(ParseCommands(content));
                SetStatus($"Loaded: {trimmed}");
            }
            catch (Exception ex)
            {
                SetStatus($"Load failed: {ex.Message}");
            }
        }

        RefreshCommandLines();
    }

    private async Task PickAndLoadScriptAsync()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "Recorder script (*.txt;*.script;*.rbs)|*.txt;*.script;*.rbs|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Open lure script"
        };

        var result = dialog.ShowDialog();
        if (result != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
            return;

        await LoadScriptAsync(dialog.FileName);
    }

    private async Task SaveScriptAsync(bool allowSaveAs)
    {
        var targetPath = _scriptPath;
        if (allowSaveAs || string.IsNullOrWhiteSpace(targetPath))
        {
            using var dialog = new Forms.SaveFileDialog
            {
                Filter = "Recorder script (*.txt)|*.txt|Script (*.script)|*.script|RBS (*.rbs)|*.rbs|All files (*.*)|*.*",
                AddExtension = true,
                DefaultExt = "txt",
                Title = "Save lure script",
                FileName = Path.GetFileName(string.IsNullOrWhiteSpace(_scriptPath) ? "lure-recorder.txt" : _scriptPath)
            };

            var dialogResult = dialog.ShowDialog();
            if (dialogResult != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                SetStatus("Save cancelled");
                return;
            }

            targetPath = dialog.FileName;
        }

        var lines = EnsureDismountFirst(_commands).Select(FormatCommandLine);
        var content = string.Join(Environment.NewLine, lines) + Environment.NewLine;

        try
        {
            await File.WriteAllTextAsync(targetPath, content);
            _scriptPath = targetPath;
            if (_onScriptSaved != null)
                await _onScriptSaved(targetPath);
            SetStatus($"Saved: {targetPath}");
        }
        catch (Exception ex)
        {
            SetStatus($"Save failed: {ex.Message}");
        }
    }

    private static IEnumerable<LureRecorderCommandEntry> EnsureDismountFirst(IEnumerable<LureRecorderCommandEntry> commands)
    {
        yield return new LureRecorderCommandEntry
        {
            Type = "dismount",
            Params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        foreach (var command in commands)
        {
            if (string.Equals(command.Type, "dismount", StringComparison.OrdinalIgnoreCase))
                continue;
            yield return command;
        }
    }

    private static List<LureRecorderCommandEntry> ParseCommands(string content)
    {
        var parsed = new List<LureRecorderCommandEntry>();
        if (string.IsNullOrWhiteSpace(content))
            return parsed;

        foreach (var rawLine in content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                continue;

            var type = tokens[0].Trim().ToLowerInvariant();
            if (!CommandTypes.Contains(type, StringComparer.OrdinalIgnoreCase) && type != "dismount")
                continue;

            var entry = new LureRecorderCommandEntry
            {
                Type = type,
                Params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            if (type is "buy" or "repair" or "cast" or "store" or "supply")
            {
                if (tokens.Length > 1)
                    entry.Params["codename"] = tokens[1];
            }
            else if (type == "teleport")
            {
                if (tokens.Length > 1)
                    entry.Params["codename"] = tokens[1];
                if (tokens.Length > 2)
                    entry.Params["destination"] = tokens[2];
            }
            else if (type == "wait")
            {
                entry.Params["time"] = tokens.Length > 1 ? tokens[1] : "1";
            }
            else if (type == "move")
            {
                if (tokens.Length > 1)
                    entry.Params["xOffset"] = tokens[1];
                if (tokens.Length > 2)
                    entry.Params["yOffset"] = tokens[2];
                if (tokens.Length > 3)
                    entry.Params["zOffset"] = tokens[3];
                if (tokens.Length > 4)
                    entry.Params["xSector"] = tokens[4];
                if (tokens.Length > 5)
                    entry.Params["ySector"] = tokens[5];
            }

            if (type != "dismount")
                parsed.Add(entry);
        }

        return parsed;
    }

    private void SetStatus(string status)
    {
        _statusLabel.Text = status;
    }

    private void HandleAutoPlayerMove()
    {
        if (!_isRecording || !IsVisible)
            return;

        var player = UBot.Core.RuntimeAccess.Session.Player;
        if (player == null)
            return;

        var position = player.Position;
        var xOffset = position.XOffset.ToString("0.00", CultureInfo.InvariantCulture);
        var yOffset = position.YOffset.ToString("0.00", CultureInfo.InvariantCulture);
        var zOffset = position.ZOffset.ToString("0.00", CultureInfo.InvariantCulture);
        var xSector = position.Region.X.ToString(CultureInfo.InvariantCulture);
        var ySector = position.Region.Y.ToString(CultureInfo.InvariantCulture);

        var signature = $"{xOffset}|{yOffset}|{zOffset}|{xSector}|{ySector}";
        if (string.Equals(signature, _lastMoveSignature, StringComparison.Ordinal))
            return;

        _lastMoveSignature = signature;
        _commands.Add(
            new LureRecorderCommandEntry
            {
                Type = "move",
                Params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["xOffset"] = xOffset,
                    ["yOffset"] = yOffset,
                    ["zOffset"] = zOffset,
                    ["xSector"] = xSector,
                    ["ySector"] = ySector
                }
            }
        );
        RefreshCommandLines();
        _commandsList.SelectedIndex = _commands.Count - 1;
        SetStatus("Recording: move");
    }

    private void HandleAutoCast(uint skillId)
    {
        if (!_isRecording || !IsVisible)
            return;

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (nowMs - _lastCastAtMs < 450)
            return;

        var skillCode = UBot.Core.RuntimeAccess.Session.Player?.Skills?.GetSkillInfoById(skillId)?.Record?.Basic_Code?.Trim();
        if (string.IsNullOrWhiteSpace(skillCode))
            skillCode = skillId.ToString(CultureInfo.InvariantCulture);

        var signature = skillCode.ToLowerInvariant();
        if (string.Equals(signature, _lastCastSignature, StringComparison.Ordinal))
            return;

        _lastCastSignature = signature;
        _lastCastAtMs = nowMs;

        _commands.Add(
            new LureRecorderCommandEntry
            {
                Type = "cast",
                Params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["codename"] = skillCode
                }
            }
        );
        RefreshCommandLines();
        _commandsList.SelectedIndex = _commands.Count - 1;
        SetStatus("Recording: cast");
    }

    private static void EnsureGlobalEventSubscriptions()
    {
        lock (GlobalSync)
        {
            if (_eventsSubscribed)
                return;

            _playerMoveHandler = OnGlobalPlayerMove;
            _skillCastHandler = OnGlobalCastSkill;
            UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnPlayerMove", _playerMoveHandler);
            UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnCastSkill", _skillCastHandler);
            _eventsSubscribed = true;
        }
    }

    private static void OnGlobalPlayerMove()
    {
        if (_activeRecorder == null || !_activeRecorder.TryGetTarget(out var recorder))
            return;

        Dispatcher.UIThread.Post(recorder.HandleAutoPlayerMove);
    }

    private static void OnGlobalCastSkill(uint skillId)
    {
        if (_activeRecorder == null || !_activeRecorder.TryGetTarget(out var recorder))
            return;

        Dispatcher.UIThread.Post(() => recorder.HandleAutoCast(skillId));
    }
}
