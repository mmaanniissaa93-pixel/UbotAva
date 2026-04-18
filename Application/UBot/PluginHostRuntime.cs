using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UBot.Core;
using UBot.Core.Plugins;

namespace UBot;

internal static class PluginHostRuntime
{
    public static int Run(string pluginName, string pluginPath)
    {
        if (string.IsNullOrWhiteSpace(pluginName))
        {
            Log.Error("Plugin host mode failed: --plugin-name is required.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(pluginPath) || !File.Exists(pluginPath))
        {
            Log.Error($"Plugin host mode failed: plugin path not found [{pluginPath}].");
            return 3;
        }

        IPlugin plugin = null;
        try
        {
            GlobalConfig.Load();

            var assembly = Assembly.LoadFrom(pluginPath);
            var pluginType = assembly
                .GetTypes()
                .FirstOrDefault(type =>
                    type.IsPublic &&
                    !type.IsAbstract &&
                    typeof(IPlugin).IsAssignableFrom(type));

            if (pluginType == null)
            {
                Log.Error($"Plugin host mode failed: no public IPlugin type found in [{Path.GetFileName(pluginPath)}].");
                return 4;
            }

            plugin = Activator.CreateInstance(pluginType) as IPlugin;
            if (plugin == null)
            {
                Log.Error($"Plugin host mode failed: could not instantiate plugin type [{pluginType.FullName}].");
                return 5;
            }

            if (!plugin.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error($"Plugin host mode failed: expected [{pluginName}] but loaded [{plugin.Name}].");
                return 6;
            }

            plugin.Enabled = true;
            plugin.Initialize();
            plugin.Enable();
            Log.Notify($"Out-of-proc host online for plugin [{plugin.Name}] (pid={Environment.ProcessId}).");

            var shutdownSignal = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdownSignal.Set();
            };
            AppDomain.CurrentDomain.ProcessExit += (_, _) => shutdownSignal.Set();

            shutdownSignal.Wait();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error($"Plugin host runtime failed [{pluginName}]: {ex.Message}");
            return 1;
        }
        finally
        {
            if (plugin != null)
            {
                try
                {
                    plugin.Disable();
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
