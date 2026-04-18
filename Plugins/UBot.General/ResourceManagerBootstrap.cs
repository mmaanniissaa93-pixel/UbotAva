using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

namespace UBot.General;

internal static class ResourceManagerBootstrap
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        ConfigureResources(
            "UBot.General.Properties.Resources",
            "UBot.General.Properties.Resources",
            typeof(ResourceManagerBootstrap).Assembly
        );
    }

    private static void ConfigureResources(string resourceTypeName, string resourceBaseName, Assembly assembly)
    {
        try
        {
            var resourceType = assembly.GetType(resourceTypeName, throwOnError: false);
            var resourceManagerField = resourceType?.GetField("resourceMan", BindingFlags.Static | BindingFlags.NonPublic);
            if (resourceManagerField == null || resourceManagerField.FieldType != typeof(ResourceManager))
                return;

            resourceManagerField.SetValue(null, new ResourceManager(resourceBaseName, assembly));
        }
        catch
        {
            // Keep startup resilient; fallback remains the generated resource manager.
        }
    }
}
