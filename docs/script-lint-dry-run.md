# Script Lint and Dry-Run

`ScriptManager` now provides two helpers before execution:

- `LintScript(...)`: validates command names and argument shapes.
- `DryRun(...)`: simulates command traversal without executing handlers.

## API

```csharp
var lint = ScriptManager.LintScript();
if (!lint.IsValid)
{
    foreach (var issue in lint.Issues)
        Console.WriteLine($"{issue.Severity} line {issue.LineNumber}: {issue.Message}");
}

var dryRun = ScriptManager.DryRun(useNearbyWaypoint: false, logSimulation: false);
Console.WriteLine($"Simulated command count: {dryRun.SimulatedCommands}");
```

## Behavior

- Unknown commands are reported as `Error`.
- Missing required arguments are reported as `Error`.
- `move` argument format is validated.
- `wait` argument must be a non-negative integer.
- `RunScript(...)` now performs an internal dry-run/lint gate and aborts on lint errors.
