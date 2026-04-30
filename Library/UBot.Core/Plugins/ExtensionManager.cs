using UBot.Core.Event;
using UBot.Core.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UBot.Core.Plugins;

public class ExtensionManager
{
    /// <summary>
    ///     Gets the extension directory for plugins.
    /// </summary>
    private static readonly string _directoryForPlugins = Path.Combine(UBot.Core.RuntimeAccess.Core.BasePath, "Data", "Plugins");

    /// <summary>
    ///     Gets the extension directory for botbases.
    /// </summary>
    private static readonly string _directoryForBotbases = Path.Combine(UBot.Core.RuntimeAccess.Core.BasePath, "Data", "Bots");

    /// <summary>
    ///     Gets the extensions.
    /// </summary>
    private static readonly List<IExtension> _extensions = [];
    private static readonly HashSet<string> _initializedExtensions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _extensionStateLock = new();
    private static readonly Dictionary<string, PluginContractManifest> _pluginContracts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _pluginAssemblyPaths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly PluginFaultIsolationManager _faultIsolation = new();
    private static readonly PluginOutOfProcessHostManager _outOfProcHostManager = new();
    private static readonly HashSet<string> _requiredOutOfProcPlugins = new(StringComparer.OrdinalIgnoreCase)
    {
        "UBot.PacketInspector",
        "UBot.AutoDungeon"
    };

    /// <summary>
    ///    Gets the plugins.
    /// </summary>
    public static IEnumerable<IPlugin> Plugins => _extensions.OfType<IPlugin>().OrderBy(p => p.Index);

    /// <summary>
    /// Gets the botbases.
    /// </summary>
    public static IEnumerable<IBotbase> Bots => _extensions.OfType<IBotbase>();

    public static string LastLoadError { get; private set; } = string.Empty;

    public static IReadOnlyDictionary<string, PluginContractManifest> PluginContracts => _pluginContracts;

    public static object GetPluginIsolationSnapshot()
    {
        return new
        {
            inProc = _faultIsolation.GetSnapshot(),
            outOfProc = _outOfProcHostManager.GetSnapshot()
        };
    }

    /// <summary>
    ///     Gets the packet handlers registered by plugins.
    /// </summary>
    private static Dictionary<string, List<IPacketHandler>> PluginHandlers { get; set; } = [];

    /// <summary>
    ///     Gets the packet hooks registered by plugins.
    /// </summary>
    private static Dictionary<string, List<IPacketHook>> PluginHooks { get; set; } = [];

    /// <summary>
    ///     HTTP client for downloading plugins from web.
    /// </summary>
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    /// <summary>
    ///     Event for download progress updates.
    /// </summary>
    public static event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;

    /// <summary>
    ///     Notifies all enabled plugins that the active profile has changed.
    /// </summary>
    public static void OnProfileChanged()
    {
        foreach (var plugin in Plugins.Where(p => p.Enabled))
        {
            try
            {
                var policy = GetRestartPolicy(plugin.Name);
                _faultIsolation.TryExecute(plugin.Name, "on-profile-changed", policy, plugin.OnProfileChanged, out _);
            }
            catch (Exception ex)
            {
                Log.Error($"Plugin [{plugin.Name}] failed to handle profile change: {ex.Message}");
            }
        }

        UBot.Core.RuntimeAccess.Events.FireEvent("OnLoadCharacter");
    }

    /// <summary>
    /// Loads the assemblies.
    /// </summary>
    public static bool LoadAssemblies<T>() where T : IExtension
    {
        try
        {
            LastLoadError = string.Empty;
            var name = typeof(T).Name;
            var directory = name == nameof(IPlugin) ? _directoryForPlugins : _directoryForBotbases;
            var files = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);

            if (name == nameof(IPlugin))
            {
                _pluginContracts.Clear();
                _pluginAssemblyPaths.Clear();
            }

            foreach (var file in files)
            {
                var loadedExtensions = GetExtensionsFromAssembly<T>(file);
                foreach (var extension in loadedExtensions)
                {
                    _extensions.Add(extension);
                    Log.Debug($"Loaded {name} [{extension.Name}]");
                }
            }

            if (name == nameof(IPlugin))
            {
                ValidatePluginContracts();
                RegisterOutOfProcHosts();
                if (!_outOfProcHostManager.StartRegisteredEnabledHosts(out var failedOutOfProcHosts))
                {
                    var failedList = string.Join(", ", failedOutOfProcHosts);
                    throw new InvalidOperationException(
                        $"Out-of-proc plugin host startup failed for: {failedList}."
                    );
                }
                UBot.Core.RuntimeAccess.Events.FireEvent("OnLoadPlugins");
            }
            else
                UBot.Core.RuntimeAccess.Events.FireEvent("OnLoadBotbases");

            return true;
        }
        catch (Exception ex)
        {
            LastLoadError = ex.Message;
            File.WriteAllText(UBot.Core.RuntimeAccess.Core.BasePath + "\\boot-error.log",
                $"The plugin manager encountered a problem: \n{ex.Message} at {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    ///     Gets the extensions from assembly.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <returns></returns>
    private static List<T> GetExtensionsFromAssembly<T>(string file)
        where T : IExtension
    {
        var result = new List<T>();

        try
        {
            var assembly = Assembly.LoadFrom(file);
            var disabledList = LoadDisabledPlugins();
            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var loadedTypes = ex.Types.Where(type => type != null).ToArray();
                var loaderExceptions = ex.LoaderExceptions ?? Array.Empty<Exception>();
                var totalTypes = ex.Types.Length;
                var failedTypes = totalTypes - loadedTypes.Length;

                Log.Warn(
                    $"Assembly type load issue. Assembly=[{Path.GetFileName(file)}], " +
                    $"LoadedTypes=[{loadedTypes.Length}], TotalTypes=[{totalTypes}], " +
                    $"FailedTypes=[{failedTypes}], LoaderExceptions=[{loaderExceptions.Length}]");

                foreach (var loaderEx in loaderExceptions)
                {
                    if (loaderEx == null)
                        continue;

                    var extraInfo = string.Empty;
                    if (loaderEx is FileNotFoundException fnfEx && !string.IsNullOrEmpty(fnfEx.FileName))
                        extraInfo = $" (File: {fnfEx.FileName})";
                    else if (!string.IsNullOrEmpty(loaderEx.StackTrace))
                        extraInfo = $" (Stack: {loaderEx.StackTrace.Split('\n').FirstOrDefault()?.Trim()})";

                    Log.Warn(
                        $"LoaderException [{Path.GetFileName(file)}]: " +
                        $"{loaderEx.GetType().Name}: {loaderEx.Message}{extraInfo}");
                }

                assemblyTypes = loadedTypes;
            }

            foreach (var type in assemblyTypes.Where(type =>
                         type.IsPublic &&
                         !type.IsAbstract &&
                         type.GetInterface(typeof(T).Name) != null))
            {
                try
                {
                    if (Activator.CreateInstance(type) is not T extension)
                        continue;

                    var plugin = extension as IExtension;
                    plugin.Enabled = !disabledList.Contains(plugin.Name);

                    if (extension is IPlugin runtimePlugin)
                    {
                        try
                        {
                            var contract = PluginContractManifestLoader.LoadForPlugin(file, runtimePlugin);
                            _pluginContracts[runtimePlugin.Name] = contract;
                            _pluginAssemblyPaths[runtimePlugin.Name] = file;
                        }
                        catch (PluginContractException ex)
                        {
                            Log.Error(
                                $"Plugin contract/manifest error. Assembly=[{Path.GetFileName(file)}], " +
                                $"Plugin=[{runtimePlugin?.Name ?? runtimePlugin?.GetType().FullName ?? "unknown"}], Error=[{ex.Message}]. " +
                                "Skipping this plugin and continuing with the remaining plugins.");
                            continue;
                        }
                    }

                    result.Add(extension);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Skipping extension type [{type.FullName}] in [{Path.GetFileName(file)}]: {ex.Message}");
                }
            }

            if (result.Count == 0)
                return result;

            var handlerType = typeof(IPacketHandler);
            var hookType = typeof(IPacketHook);

            var types = assemblyTypes
                .Where(p => handlerType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
            var shouldRegisterNetworkHandlers = result.Any(extension =>
            {
                if (!extension.Enabled)
                    return false;

                if (extension is IPlugin plugin)
                    return !IsOutOfProcPlugin(plugin.Name);

                return true;
            });

            var handlers = new List<IPacketHandler>();
            foreach (var handler in types)
            {
                try
                {
                    if (Activator.CreateInstance(handler) is not IPacketHandler instance)
                        continue;

                    if (shouldRegisterNetworkHandlers)
                        UBot.Core.RuntimeAccess.Packets.RegisterHandler(instance);

                    handlers.Add(instance);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Skipping packet handler [{handler.FullName}] in [{Path.GetFileName(file)}]: {ex.Message}");
                }
            }

            types = assemblyTypes
                .Where(p => hookType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            var hooks = new List<IPacketHook>();
            foreach (var hook in types)
            {
                try
                {
                    if (Activator.CreateInstance(hook) is not IPacketHook instance)
                        continue;

                    if (shouldRegisterNetworkHandlers)
                        UBot.Core.RuntimeAccess.Packets.RegisterHook(instance);

                    hooks.Add(instance);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Skipping packet hook [{hook.FullName}] in [{Path.GetFileName(file)}]: {ex.Message}");
                }
            }

            // Store handlers and hooks for each plugin
            foreach (var plugin in result)
            {
                PluginHandlers[plugin.Name] = handlers;
                PluginHooks[plugin.Name] = hooks;
            }
        }
        catch (Exception ex)
        {
            if (ex is PluginContractException contractEx)
            {
                Log.Error(
                    $"Plugin contract/manifest error during assembly inspection. Assembly=[{Path.GetFileName(file)}], Error=[{contractEx.Message}]. " +
                    "Skipping this assembly and continuing with the remaining plugins.");
                return new List<T>();
            }

            Log.Error($"Failed to inspect assembly [{Path.GetFileName(file)}]: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    ///     Initializes the extension only once for the current process lifetime.
    /// </summary>
    /// <param name="extension">The extension.</param>
    /// <returns>True if the extension is initialized or was already initialized.</returns>
    private static bool EnsureInitialized(IExtension extension)
    {
        if (extension == null)
            return false;

        if (extension is IPlugin plugin && IsOutOfProcPlugin(plugin.Name))
            return true;

        lock (_extensionStateLock)
        {
            if (_initializedExtensions.Contains(extension.Name))
                return true;

            var policy = GetRestartPolicy(extension.Name);
            if (!_faultIsolation.TryExecute(extension.Name, "initialize", policy, extension.Initialize, out var failure))
            {
                Log.Error($"Plugin [{extension.Name}] initialization failed: {failure?.Message}");
                return false;
            }

            _initializedExtensions.Add(extension.Name);
            return true;
        }
    }

    /// <summary>
    ///     Initializes an extension if it was not initialized before.
    /// </summary>
    /// <param name="extension">The extension.</param>
    /// <returns>True if initialization succeeded or was already done.</returns>
    public static bool InitializeExtension(IExtension extension)
    {
        return EnsureInitialized(extension);
    }

    /// <summary>
    /// Returns the directory path associated with the specified extension type parameter.
    /// </summary>
    /// <typeparam name="T">The extension type for which to retrieve the directory path. Must implement the IExtension interface.</typeparam>
    /// <returns>The directory path for the specified extension type. Returns the plugin directory if T is IPlugin; otherwise,
    /// returns the botbase directory.</returns>
    private static string getPath<T>() where T : IExtension
    {
        return typeof(T) == typeof(IPlugin) ? _directoryForPlugins : _directoryForBotbases;
    }

    /// <summary>
    ///     Downloads a plugin from a URL.
    /// </summary>
    /// <param name="url">The URL to download from.</param>
    /// <param name="targetFileName">The target file name (without path).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the downloaded file, or null if failed.</returns>
    public static async Task<string> DownloadPluginFromWeb<T>(string url, string targetFileName = null, CancellationToken cancellationToken = default)
        where T : IExtension
    {
        try
        {
            Log.Notify($"Starting download from: {url}");

            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var fileName = targetFileName ?? Path.GetFileName(url);

            if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                fileName = "plugin_" + DateTime.Now.Ticks + ".dll";

            var targetPath = Path.Combine(getPath<T>(), fileName);
            var tempPath = targetPath + ".tmp";

            using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        var progress = (int)((totalRead * 100) / totalBytes);
                        DownloadProgressChanged?.Invoke(null, new DownloadProgressEventArgs
                        {
                            ProgressPercentage = progress,
                            BytesReceived = totalRead,
                            TotalBytesToReceive = totalBytes,
                            FileName = fileName
                        });
                    }
                }
            }

            // Move temp file to final location
            if (File.Exists(targetPath))
                File.Delete(targetPath);

            File.Move(tempPath, targetPath);

            Log.Notify($"Plugin downloaded successfully: {targetPath}");
            return targetPath;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to download plugin from {url}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Downloads and installs a plugin from a URL.
    /// </summary>
    /// <param name="url">The URL to download from.</param>
    /// <param name="autoLoad">If true, loads the plugin immediately after download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public static async Task<bool> DownloadAndInstallPlugin<T>(
        string url,
        string expectedHash,
        bool autoLoad = true,
        CancellationToken cancellationToken = default
    )
        where T : IExtension
    {
        try
        {
            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                Log.Error("Plugin installation denied: SHA256 hash is required.");
                return false;
            }

            var downloadedPath = await DownloadPluginFromWeb<T>(url, null, cancellationToken);

            if (string.IsNullOrEmpty(downloadedPath))
                return false;

            if (!VerifyPluginHash(downloadedPath, expectedHash))
            {
                Log.Error($"Plugin installation denied: SHA256 mismatch for '{downloadedPath}'.");

                try
                {
                    File.Delete(downloadedPath);
                }
                catch
                {
                    /* ignore cleanup failures */
                }

                return false;
            }

            if (autoLoad)
            {
                return LoadPluginFromFile<T>(downloadedPath);
            }

            Log.Notify("Plugin downloaded. Restart required to load it.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to download and install plugin: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Verifies a plugin file's integrity using SHA256 hash.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="expectedHash">The expected SHA256 hash (hex string).</param>
    /// <returns>True if hash matches, otherwise false.</returns>
    public static bool VerifyPluginHash(string filePath, string expectedHash)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(expectedHash))
                return false;

            var normalizedExpectedHash = NormalizeHash(expectedHash);
            if (normalizedExpectedHash.Length != 64)
                return false;

            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            return hashString.Equals(normalizedExpectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeHash(string hash)
    {
        return new string(hash.Where(Uri.IsHexDigit).ToArray()).ToLowerInvariant();
    }

    /// <summary>
    ///     Loads a plugin repository from a JSON URL.
    /// </summary>
    /// <param name="repositoryUrl">The repository JSON URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded repository, or null if failed.</returns>
    public static async Task<PluginRepository> LoadRepositoryFromUrl(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.Notify($"Loading plugin repository from: {repositoryUrl}");

            var json = await HttpClient.GetStringAsync(repositoryUrl, cancellationToken);
            var repository = JsonSerializer.Deserialize<PluginRepository>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (repository == null)
            {
                Log.Error("Failed to parse repository JSON");
                return null;
            }

            Log.Notify($"Loaded repository '{repository.Name}' with {repository.Plugins.Count} plugins");
            return repository;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load repository: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Enables a plugin.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <returns>True if successfully enabled, otherwise false.</returns>
    public static bool EnablePlugin(string internalName)
    {
        if (!_extensions.Any(p => p.Name == internalName))
            return false;

        var plugin = _extensions.FirstOrDefault(p => p.Name == internalName);
        if (plugin.Enabled)
        {
            Log.Warn($"Plugin [{plugin.Title}] is already enabled. Skipping duplicate enable.");
            return true;
        }

        try
        {
            if (plugin is IPlugin outOfProcCandidate && IsOutOfProcPlugin(outOfProcCandidate.Name))
            {
                if (!_outOfProcHostManager.Enable(outOfProcCandidate.Name))
                    return false;

                plugin.Enabled = true;
                SaveDisabledPlugins();
                UBot.Core.RuntimeAccess.Events.FireEvent("OnPluginEnabled", plugin);
                Log.Notify($"Plugin [{plugin.Title}] enabled in out-of-proc mode.");
                return true;
            }

            if (!EnsureInitialized(plugin))
                return false;

            var policy = GetRestartPolicy(plugin.Name);
            if (!_faultIsolation.TryExecute(plugin.Name, "enable", policy, plugin.Enable, out var failure))
            {
                Log.Error(
                    $"Operation=[EnablePlugin], Plugin=[{plugin.Title}], InternalName=[{internalName}], " +
                    $"Type=[{plugin.GetType().FullName}], Failure=[{failure?.Message}]");
                return false;
            }

            plugin.Enabled = true;

            RegisterPluginPacketRegistrations(internalName);

            SaveDisabledPlugins(); // Save state
            UBot.Core.RuntimeAccess.Events.FireEvent("OnPluginEnabled", plugin);
            Log.Notify($"Plugin [{plugin.Title}] enabled!");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(
                $"Operation=[EnablePlugin], Plugin=[{plugin.Title}], InternalName=[{internalName}], " +
                $"Type=[{plugin.GetType().FullName}], Exception=[{ex.GetType().Name}], " +
                $"Message=[{ex.Message}], StackTrace=[{ex.StackTrace}]");
            return false;
        }
    }

    /// <summary>
    ///     Disables a plugin.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <returns>True if successfully disabled, otherwise false.</returns>
    public static bool DisablePlugin(string internalName)
    {
        if (!_extensions.Any(p => p.Name == internalName))
            return false;

        var plugin = _extensions.FirstOrDefault(p => p.Name == internalName);
        if (!plugin.Enabled)
        {
            Log.Warn($"Plugin [{plugin.Title}] is already disabled. Skipping duplicate disable.");
            return true;
        }

        try
        {
            if (plugin is IPlugin outOfProcCandidate && IsOutOfProcPlugin(outOfProcCandidate.Name))
            {
                _outOfProcHostManager.Disable(outOfProcCandidate.Name);
                plugin.Enabled = false;
                SaveDisabledPlugins();
                UBot.Core.RuntimeAccess.Events.FireEvent("OnPluginDisabled", plugin);
                Log.Notify($"Plugin [{plugin.Title}] disabled in out-of-proc mode.");
                return true;
            }

            var policy = GetRestartPolicy(plugin.Name);
            if (!_faultIsolation.TryExecute(plugin.Name, "disable", policy, plugin.Disable, out var failure))
            {
                Log.Error(
                    $"Operation=[DisablePlugin], Plugin=[{plugin.Title}], InternalName=[{internalName}], " +
                    $"Type=[{plugin.GetType().FullName}], Failure=[{failure?.Message}]");

                TryCleanupPluginPacketRegistrations(internalName, plugin);
                return false;
            }

            plugin.Enabled = false;

            RemovePluginPacketRegistrations(internalName);

            SaveDisabledPlugins(); // Save state
            UBot.Core.RuntimeAccess.Events.FireEvent("OnPluginDisabled", plugin);
            Log.Notify($"Plugin [{plugin.Title}] disabled!");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(
                $"Operation=[DisablePlugin], Plugin=[{plugin.Title}], InternalName=[{internalName}], " +
                $"Type=[{plugin.GetType().FullName}], Exception=[{ex.GetType().Name}], " +
                $"Message=[{ex.Message}], StackTrace=[{ex.StackTrace}]");

            TryCleanupPluginPacketRegistrations(internalName, plugin);
            return false;
        }
    }

    /// <summary>
    ///     Toggles a plugin's enabled state.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <returns>The new enabled state.</returns>
    public static bool TogglePlugin(string internalName)
    {
        if (!_extensions.Any(p => p.Name == internalName))
            return false;

        var plugin = _extensions.FirstOrDefault(p => p.Name == internalName);

        return plugin.Enabled ? DisablePlugin(internalName) : EnablePlugin(internalName);
    }

    /// <summary>
    ///     Reloads a plugin.
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <returns>True if successfully reloaded, otherwise false.</returns>
    public static bool ReloadPlugin(string internalName)
    {
        if (!_extensions.Any(p => p.Name == internalName))
            return false;

        var plugin = _extensions.FirstOrDefault(p => p.Name == internalName);
        var wasEnabled = plugin.Enabled;

        if (!DisablePlugin(internalName))
            return false;

        if (wasEnabled)
            return EnablePlugin(internalName);

        return true;
    }

    /// <summary>
    ///     Loads a plugin from a file at runtime.
    /// </summary>
    /// <param name="filePath">The path to the plugin DLL file.</param>
    /// <returns>True if successfully loaded, otherwise false.</returns>
    public static bool LoadPluginFromFile<T>(string filePath)
        where T : IExtension
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Log.Error($"Plugin file not found: {filePath}");
                return false;
            }

            var newPlugins = GetExtensionsFromAssembly<T>(filePath);

            if (newPlugins.Count == 0)
            {
                Log.Error($"No valid plugins found in: {filePath}");
                return false;
            }


            foreach (var plugin in newPlugins)
            {
                if (_extensions.Any(p => p.Name == plugin.Name))
                {
                    Log.Warn($"Plugin '{plugin.Name}' is already loaded. Skipping.");
                    continue;
                }

                _extensions.Add(plugin);
                if (plugin is IPlugin)
                {
                    ValidatePluginContracts();
                    RegisterOutOfProcHosts();
                    if (!_outOfProcHostManager.StartRegisteredEnabledHosts(out var failedOutOfProcHosts))
                    {
                        var failedList = string.Join(", ", failedOutOfProcHosts);
                        Log.Error($"Failed to start out-of-proc plugin hosts: {failedList}");
                        return false;
                    }
                }

                if (plugin.Enabled)
                    EnsureInitialized(plugin);

                Log.Notify($"Plugin '{plugin.Title}' loaded successfully!");
                if (plugin.Enabled)
                    UBot.Core.RuntimeAccess.Events.FireEvent("OnPluginLoaded", plugin);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load plugin from {filePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Unloads a plugin (removes it from the manager).
    /// </summary>
    /// <param name="internalName">The internal name of the plugin.</param>
    /// <returns>True if successfully unloaded, otherwise false.</returns>
    public static bool UnloadPlugin(string internalName)
    {
        if (!_extensions.Any(p => p.Name == internalName))
            return false;

        try
        {
            var plugin = _extensions.FirstOrDefault(p => p.Name == internalName);

            // Disable first
            if (plugin.Enabled)
            {
                if (!DisablePlugin(internalName))
                    return false;
            }

            // Remove from extensions
            _extensions.Remove(plugin);

            lock (_extensionStateLock)
                _initializedExtensions.Remove(plugin.Name);

            // Remove handlers and hooks
            RemovePluginPacketRegistrations(internalName);

            if (PluginHandlers.ContainsKey(internalName))
                PluginHandlers.Remove(internalName);

            if (PluginHooks.ContainsKey(internalName))
                PluginHooks.Remove(internalName);

            _pluginContracts.Remove(internalName);
            _pluginAssemblyPaths.Remove(internalName);
            _outOfProcHostManager.Unregister(internalName);

            UBot.Core.RuntimeAccess.Events.FireEvent("OnPluginUnloaded", plugin);
            Log.Notify($"Plugin '{plugin.Title}' unloaded successfully!");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to unload plugin '{internalName}': {ex.Message}");
            return false;
        }
    }

    private static void RegisterOutOfProcHosts()
    {
        foreach (var plugin in Plugins)
        {
            if (!IsOutOfProcPlugin(plugin.Name))
            {
                _outOfProcHostManager.Unregister(plugin.Name);
                continue;
            }

            if (!_pluginAssemblyPaths.TryGetValue(plugin.Name, out var pluginPath))
                continue;

            if (!_pluginContracts.TryGetValue(plugin.Name, out var contract))
                continue;

            _outOfProcHostManager.Register(plugin.Name, pluginPath, contract, plugin.Enabled);
        }
    }

    private static bool IsOutOfProcPlugin(string pluginName)
    {
        if (string.IsNullOrWhiteSpace(pluginName))
            return false;

        if (!_pluginContracts.TryGetValue(pluginName, out var contract))
            return false;

        return string.Equals(contract.Isolation?.Mode, "outproc", StringComparison.OrdinalIgnoreCase);
    }

    private static PluginRestartPolicyManifest GetRestartPolicy(string pluginName)
    {
        if (string.IsNullOrWhiteSpace(pluginName))
            return new PluginRestartPolicyManifest();

        if (_pluginContracts.TryGetValue(pluginName, out var contract))
            return contract.Isolation?.RestartPolicy ?? new PluginRestartPolicyManifest();

        return new PluginRestartPolicyManifest();
    }

    private static void ValidatePluginContracts()
    {
        var hostVersion = GetHostVersion();

        foreach (var plugin in Plugins)
        {
            if (!_pluginContracts.TryGetValue(plugin.Name, out var contract))
                throw new InvalidOperationException($"Plugin [{plugin.Name}] contract manifest is not loaded.");

            if (!string.IsNullOrWhiteSpace(contract.HostCompatibility?.MinVersion) ||
                !string.IsNullOrWhiteSpace(contract.HostCompatibility?.MaxVersionExclusive))
            {
                var isHostCompatible = PluginVersionRange.Satisfies(
                    hostVersion,
                    contract.HostCompatibility.MinVersion,
                    contract.HostCompatibility.MaxVersionExclusive
                );
                if (!isHostCompatible)
                {
                    throw new InvalidOperationException(
                        $"Plugin [{plugin.Name}] is not compatible with host version [{hostVersion}]. " +
                        $"Allowed range: >= {contract.HostCompatibility.MinVersion} and < {contract.HostCompatibility.MaxVersionExclusive}."
                    );
                }
            }

            if (_requiredOutOfProcPlugins.Contains(plugin.Name)
                && !string.Equals(contract.Isolation?.Mode, "outproc", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Plugin [{plugin.Name}] must run in out-of-proc mode. Update manifest isolation.mode to 'outproc'."
                );
            }
        }

        foreach (var plugin in Plugins)
        {
            if (!_pluginContracts.TryGetValue(plugin.Name, out var contract))
                continue;

            foreach (var dependency in contract.Dependencies ?? [])
            {
                if (dependency == null || string.IsNullOrWhiteSpace(dependency.PluginName))
                    continue;

                if (!_pluginContracts.TryGetValue(dependency.PluginName, out var dependencyContract))
                {
                    if (dependency.Required)
                    {
                        throw new InvalidOperationException(
                            $"Plugin [{plugin.Name}] requires missing dependency [{dependency.PluginName}]."
                        );
                    }

                    continue;
                }

                if (!PluginVersionRange.Satisfies(
                        dependencyContract.PluginVersion,
                        dependency.MinVersion,
                        dependency.MaxVersionExclusive))
                {
                    throw new InvalidOperationException(
                        $"Plugin [{plugin.Name}] dependency [{dependency.PluginName}] has incompatible version [{dependencyContract.PluginVersion}]."
                    );
                }

                if (dependency.RequiredCapabilities == null || dependency.RequiredCapabilities.Length == 0)
                    continue;

                var capabilitySet = new HashSet<string>(dependencyContract.Capabilities ?? [], StringComparer.OrdinalIgnoreCase);
                foreach (var requiredCapability in dependency.RequiredCapabilities)
                {
                    if (string.IsNullOrWhiteSpace(requiredCapability))
                        continue;

                    if (!capabilitySet.Contains(requiredCapability))
                    {
                        throw new InvalidOperationException(
                            $"Plugin [{plugin.Name}] requires capability [{requiredCapability}] from dependency [{dependency.PluginName}]."
                        );
                    }
                }
            }
        }
    }

    private static string GetHostVersion()
    {
        var hostAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var hostVersion = hostAssembly.GetName().Version?.ToString();
        if (!string.IsNullOrWhiteSpace(hostVersion))
            return hostVersion;

        return "0.0.0";
    }

    public static void Shutdown()
    {
        try
        {
            _outOfProcHostManager.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to shutdown out-of-proc plugin hosts: {ex.Message}");
        }
    }


    /// <summary>
    /// Get the disabled plugins list from config.
    /// </summary>
    private static string[] LoadDisabledPlugins()
    {
        var list = UBot.Core.RuntimeAccess.Global.Get("UBot.DisabledPlugins", "");

        return list.Split(",", StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Saves the disabled plugins list to config.
    /// </summary>
    private static void SaveDisabledPlugins()
    {
        var disabledPlugins = _extensions
            .Where(p => !p.Enabled)
            .Select(p => p.Name)
            .ToArray();

        UBot.Core.RuntimeAccess.Global.Set("UBot.DisabledPlugins", string.Join(",", disabledPlugins));
        UBot.Core.RuntimeAccess.Global.Save();
    }

    private static bool TryCleanupPluginPacketRegistrations(string internalName, IExtension extension)
    {
        var success = true;

        try
        {
            RemovePluginPacketRegistrations(internalName);
        }
        catch (Exception ex)
        {
            success = false;
            Log.Error(
                $"Operation=[CleanupPluginHandlers], Plugin=[{extension.Title}], InternalName=[{internalName}], " +
                $"Type=[{extension.GetType().FullName}], Exception=[{ex.GetType().Name}], " +
                $"Message=[{ex.Message}], StackTrace=[{ex.StackTrace}]");
        }

        return success;
    }

    private static void RegisterPluginPacketRegistrations(string internalName)
    {
        if (PluginHandlers.TryGetValue(internalName, out var handlers))
        {
            foreach (var handler in handlers)
                UBot.Core.RuntimeAccess.Packets.RegisterHandler(handler);
        }

        if (PluginHooks.TryGetValue(internalName, out var hooks))
        {
            foreach (var hook in hooks)
                UBot.Core.RuntimeAccess.Packets.RegisterHook(hook);
        }
    }

    private static void RemovePluginPacketRegistrations(string internalName)
    {
        if (PluginHandlers.TryGetValue(internalName, out var handlers))
        {
            foreach (var handler in handlers)
            {
                if (!HasEnabledPeerUsingHandler(internalName, handler))
                    UBot.Core.RuntimeAccess.Packets.RemoveHandler(handler);
            }
        }

        if (PluginHooks.TryGetValue(internalName, out var hooks))
        {
            foreach (var hook in hooks)
            {
                if (!HasEnabledPeerUsingHook(internalName, hook))
                    UBot.Core.RuntimeAccess.Packets.RemoveHook(hook);
            }
        }
    }

    private static bool HasEnabledPeerUsingHandler(string internalName, IPacketHandler handler)
    {
        if (handler == null)
            return false;

        foreach (var entry in PluginHandlers)
        {
            if (string.Equals(entry.Key, internalName, StringComparison.OrdinalIgnoreCase))
                continue;

            var extension = _extensions.FirstOrDefault(p => string.Equals(p.Name, entry.Key, StringComparison.OrdinalIgnoreCase));
            if (extension?.Enabled != true)
                continue;

            if (entry.Value.Contains(handler))
                return true;
        }

        return false;
    }

    private static bool HasEnabledPeerUsingHook(string internalName, IPacketHook hook)
    {
        if (hook == null)
            return false;

        foreach (var entry in PluginHooks)
        {
            if (string.Equals(entry.Key, internalName, StringComparison.OrdinalIgnoreCase))
                continue;

            var extension = _extensions.FirstOrDefault(p => string.Equals(p.Name, entry.Key, StringComparison.OrdinalIgnoreCase));
            if (extension?.Enabled != true)
                continue;

            if (entry.Value.Contains(hook))
                return true;
        }

        return false;
    }
}
