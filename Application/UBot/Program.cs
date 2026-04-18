using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;

namespace UBot;

internal static class Program
{
    public static string AssemblyTitle = Branding.DisplayName;

    public static string AssemblyVersion =
        $"v{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version}";

    public static string AssemblyDescription = Branding.Description;

    public class CommandLineOptions
    {
        [Option('c', "character", Required = false, HelpText = "Set the character name to use.")]
        public string Character { get; set; }

        [Option('p', "profile", Required = false, HelpText = "Set the profile name to use.")]
        public string Profile { get; set; }

        [Option("launch-client", Required = false, HelpText = "Start with client")]
        public bool LaunchClient { get; set; }

        [Option("launch-clientless", Required = false, HelpText = "Start clientless")]
        public bool LaunchClientless { get; set; }

        [Option("plugin-host", Required = false, HelpText = "Run isolated plugin host mode")]
        public bool PluginHost { get; set; }

        [Option("plugin-name", Required = false, HelpText = "Internal plugin name for plugin host mode")]
        public string PluginName { get; set; }

        [Option("plugin-path", Required = false, HelpText = "Absolute plugin assembly path for plugin host mode")]
        public string PluginPath { get; set; }
    }

    private static void DisplayHelp(ParserResult<CommandLineOptions> result)
    {
        var helpText = HelpText.AutoBuild(
            result,
            h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.AddDashesToOption = true;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }
        );
        MessageBox.Show(
            helpText,
            AssemblyTitle + " " + AssemblyVersion,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    [STAThread]
    private static void Main(string[] args)
    {
        ProcessLifetimeManager.TryEnableChildProcessTerminationOnExit();

        var parser = new Parser(with => with.HelpWriter = Console.Out);
        var parserResult = parser.ParseArguments<CommandLineOptions>(args);
        CommandLineOptions parsedOptions = null;

        parserResult
            .WithParsed(options =>
            {
                parsedOptions = options;
                RunOptions(options);
            })
            .WithNotParsed(_ =>
            {
                DisplayHelp(parserResult);
                Environment.Exit(1);
            });

        // We need "." instead of "," while saving float numbers.
        // Also client data is "." based float digit numbers.
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (parsedOptions?.PluginHost == true)
        {
            Environment.ExitCode = PluginHostRuntime.Run(parsedOptions.PluginName, parsedOptions.PluginPath);
            return;
        }

        try
        {
            UBot.Avalonia.AvaloniaHost.Run(args);
        }
        finally
        {
            PerformFinalShutdown();
        }
    }

    private static void PerformFinalShutdown()
    {
        try
        {
            if (Kernel.Bot != null && Kernel.Bot.Running)
                Kernel.Bot.Stop();
        }
        catch (Exception ex)
        {
            Log.Warn($"Final shutdown failed while stopping bot: {ex.Message}");
        }

        try
        {
            Kernel.Proxy?.Shutdown();
            ClientManager.Kill();
        }
        catch (Exception ex)
        {
            Log.Warn($"Final shutdown failed while closing client/proxy: {ex.Message}");
        }

        try
        {
            ExtensionManager.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Warn($"Final shutdown failed while closing extension hosts: {ex.Message}");
        }

        try
        {
            GlobalConfig.Save();
        }
        catch (Exception ex)
        {
            Log.Warn($"Final shutdown failed while saving global config: {ex.Message}");
        }

        try
        {
            PlayerConfig.Save();
        }
        catch (Exception ex)
        {
            Log.Warn($"Final shutdown failed while saving player config: {ex.Message}");
        }
    }

    private static void RunOptions(CommandLineOptions options)
    {
        if (options.PluginHost)
            return;

        if (options.LaunchClient)
        {
            Kernel.LaunchMode = "client";
            Log.Debug("Launching with client dictated by launch paramaters");
        }
        else if (options.LaunchClientless)
        {
            Kernel.LaunchMode = "clientless";
            Log.Debug("Launching client as clientless dictated by launch paramaters");
        }

        if (!string.IsNullOrEmpty(options.Profile))
        {
            var profile = options.Profile;
            if (ProfileManager.ProfileExists(profile))
                ProfileManager.SetSelectedProfile(profile);
            else
                ProfileManager.Add(profile);

            ProfileManager.IsProfileLoadedByArgs = true;
            Log.Debug($"Selected profile by args: {profile}");
        }

        if (!string.IsNullOrEmpty(options.Character))
        {
            var character = options.Character;
            ProfileManager.SelectedCharacter = character;
            Log.Debug($"Selected character by args: {character}");
        }
    }
}
