using System;
using System.Collections.Generic;
using System.Linq;
using UBot.Core.Components;
using UBot.Core.Components.Scripting;
using Xunit;

namespace UBot.Core.Tests;

public class ScriptManagerValidationTests
{
    public static IEnumerable<object[]> LintScriptCases()
    {
        yield return
        [
            new[] { "", "   ", "// comment", "# comment" },
            true,
            0,
            null,
            null,
        ];

        yield return
        [
            new[] { "unknown" },
            false,
            1,
            1,
            "No script command handler found.",
        ];

        yield return
        [
            new[] { "echo" },
            false,
            1,
            1,
            "Missing arguments.",
        ];

        yield return
        [
            new[] { "move abc 2 3 4 5" },
            false,
            1,
            1,
            "Invalid move argument format.",
        ];

        yield return
        [
            new[] { "wait -1" },
            false,
            1,
            1,
            "Wait value cannot be negative.",
        ];
    }

    [Theory]
    [MemberData(nameof(LintScriptCases))]
    public void LintScript_ShouldValidateTableDrivenScenarios(
        string[] script,
        bool expectedValid,
        int expectedIssueCount,
        int? expectedLineNumber,
        string expectedMessageFragment
    )
    {
        ConfigureHandlers();

        var result = ScriptManager.LintScript(script);

        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(expectedIssueCount, result.Issues.Count);

        if (expectedLineNumber.HasValue)
            Assert.Equal(expectedLineNumber.Value, result.Issues[0].LineNumber);

        if (!string.IsNullOrEmpty(expectedMessageFragment))
            Assert.Contains(expectedMessageFragment, result.Issues[0].Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DryRun_ShouldCountExecutableCommands_AndUseStartLineZero_WhenCommandsProvided(
        bool useNearbyWaypoint
    )
    {
        ConfigureHandlers();

        var script = new[]
        {
            "// comment",
            "move 10 20 30 40 50",
            "",
            "# comment",
            "wait 100",
            "echo hello",
        };

        var result = ScriptManager.DryRun(
            useNearbyWaypoint: useNearbyWaypoint,
            commands: script,
            logSimulation: false
        );

        Assert.True(result.IsValid);
        Assert.Equal(0, result.StartLineIndex);
        Assert.Equal(3, result.SimulatedCommands);
    }

    private static void ConfigureHandlers()
    {
        ResetHandlers();

        ScriptManager.RegisterCommandHandler(new FakeScriptCommand("move", 5));
        ScriptManager.RegisterCommandHandler(new FakeScriptCommand("wait", 1));
        ScriptManager.RegisterCommandHandler(new FakeScriptCommand("echo", 1));
    }

    private static void ResetHandlers()
    {
        var commandNames = ScriptManager.CommandHandlers?.Select(command => command.Name).ToArray() ?? [];

        foreach (var commandName in commandNames)
            ScriptManager.UnregisterCommandHandler(commandName);
    }

    private sealed class FakeScriptCommand(string name, int argumentCount) : IScriptCommand
    {
        public string Name { get; } = name;

        public bool IsBusy => false;

        public Dictionary<string, string> Arguments { get; } = Enumerable
            .Range(0, argumentCount)
            .ToDictionary(index => $"arg{index}", _ => string.Empty);

        public bool Execute(string[] arguments = null)
        {
            return true;
        }

        public void Stop() { }
    }
}
