using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using UBot.Core.Network;
using UBot.Core.Plugins;
using Xunit;

namespace UBot.Core.Tests;

public class ExtensionManagerTests
{
    [Fact]
    public void DisablePlugin_ShouldKeepSharedPacketRegistrations_WhenPeerPluginStillEnabled()
    {
        var extensionState = ExtensionManagerState.CaptureAndClear();
        var packetState = PacketManagerState.CaptureAndClear();

        try
        {
            var pluginA = new TestPlugin("Plugin.A", enabled: true);
            var pluginB = new TestPlugin("Plugin.B", enabled: true);
            var handler = new TestHandler(0x1234, PacketDestination.Client);
            var hook = new TestHook(0x1234, PacketDestination.Client);

            extensionState.Extensions.Add(pluginA);
            extensionState.Extensions.Add(pluginB);
            extensionState.PluginHandlers["Plugin.A"] = new List<IPacketHandler> { handler };
            extensionState.PluginHandlers["Plugin.B"] = new List<IPacketHandler> { handler };
            extensionState.PluginHooks["Plugin.A"] = new List<IPacketHook> { hook };
            extensionState.PluginHooks["Plugin.B"] = new List<IPacketHook> { hook };

            PacketManager.RegisterHandler(handler);
            PacketManager.RegisterHook(hook);

            Assert.True(ExtensionManager.DisablePlugin("Plugin.A"));

            Assert.False(pluginA.Enabled);
            Assert.True(pluginB.Enabled);
            Assert.Contains(handler, PacketManager.GetHandlers());
            Assert.Contains(hook, PacketManager.GetHooks());

            Assert.True(ExtensionManager.DisablePlugin("Plugin.B"));

            Assert.DoesNotContain(handler, PacketManager.GetHandlers());
            Assert.DoesNotContain(hook, PacketManager.GetHooks());
        }
        finally
        {
            packetState.Restore();
            extensionState.Restore();
        }
    }

    private sealed class ExtensionManagerState
    {
        private static readonly FieldInfo ExtensionsField =
            typeof(ExtensionManager).GetField("_extensions", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly PropertyInfo PluginHandlersProperty =
            typeof(ExtensionManager).GetProperty("PluginHandlers", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly PropertyInfo PluginHooksProperty =
            typeof(ExtensionManager).GetProperty("PluginHooks", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly List<IExtension> _originalExtensions;
        private readonly Dictionary<string, List<IPacketHandler>> _originalPluginHandlers;
        private readonly Dictionary<string, List<IPacketHook>> _originalPluginHooks;

        private ExtensionManagerState(
            List<IExtension> originalExtensions,
            Dictionary<string, List<IPacketHandler>> originalPluginHandlers,
            Dictionary<string, List<IPacketHook>> originalPluginHooks
        )
        {
            _originalExtensions = originalExtensions;
            _originalPluginHandlers = originalPluginHandlers;
            _originalPluginHooks = originalPluginHooks;
            Extensions = (List<IExtension>)ExtensionsField.GetValue(null);
            PluginHandlers = new Dictionary<string, List<IPacketHandler>>(StringComparer.OrdinalIgnoreCase);
            PluginHooks = new Dictionary<string, List<IPacketHook>>(StringComparer.OrdinalIgnoreCase);
        }

        public List<IExtension> Extensions { get; }
        public Dictionary<string, List<IPacketHandler>> PluginHandlers { get; }
        public Dictionary<string, List<IPacketHook>> PluginHooks { get; }

        public static ExtensionManagerState CaptureAndClear()
        {
            var extensions = (List<IExtension>)ExtensionsField.GetValue(null);
            var originalExtensions = extensions.ToList();
            var originalPluginHandlers = (Dictionary<string, List<IPacketHandler>>)PluginHandlersProperty.GetValue(null);
            var originalPluginHooks = (Dictionary<string, List<IPacketHook>>)PluginHooksProperty.GetValue(null);
            var state = new ExtensionManagerState(originalExtensions, originalPluginHandlers, originalPluginHooks);

            extensions.Clear();
            PluginHandlersProperty.SetValue(null, state.PluginHandlers);
            PluginHooksProperty.SetValue(null, state.PluginHooks);

            return state;
        }

        public void Restore()
        {
            Extensions.Clear();
            Extensions.AddRange(_originalExtensions);
            PluginHandlersProperty.SetValue(null, _originalPluginHandlers);
            PluginHooksProperty.SetValue(null, _originalPluginHooks);
        }
    }

    private sealed class PacketManagerState
    {
        private readonly List<IPacketHandler> _handlers;
        private readonly List<IPacketHook> _hooks;

        private PacketManagerState(List<IPacketHandler> handlers, List<IPacketHook> hooks)
        {
            _handlers = handlers;
            _hooks = hooks;
        }

        public static PacketManagerState CaptureAndClear()
        {
            var handlers = PacketManager.GetHandlers();
            var hooks = PacketManager.GetHooks();

            foreach (var handler in handlers)
                PacketManager.RemoveHandler(handler);

            foreach (var hook in hooks)
                PacketManager.RemoveHook(hook);

            return new PacketManagerState(handlers, hooks);
        }

        public void Restore()
        {
            foreach (var handler in PacketManager.GetHandlers())
                PacketManager.RemoveHandler(handler);

            foreach (var hook in PacketManager.GetHooks())
                PacketManager.RemoveHook(hook);

            foreach (var handler in _handlers)
                PacketManager.RegisterHandler(handler);

            foreach (var hook in _hooks)
                PacketManager.RegisterHook(hook);
        }
    }

    private sealed class TestPlugin : IPlugin
    {
        public TestPlugin(string name, bool enabled)
        {
            Name = name;
            Enabled = enabled;
        }

        public string Author => "Test";
        public string Description => "Test";
        public string Name { get; }
        public string Title => Name;
        public string Version => "1.0.0";
        public bool Enabled { get; set; }
        public Control View => null;
        public bool DisplayAsTab => false;
        public int Index => 0;
        public bool RequireIngame => false;
        public int DisableCount { get; private set; }

        public void Initialize()
        {
        }

        public void Translate()
        {
        }

        public void Enable()
        {
        }

        public void Disable()
        {
            DisableCount++;
        }

        public void OnLoadCharacter()
        {
        }
    }

    private sealed class TestHandler : IPacketHandler
    {
        public TestHandler(ushort opcode, PacketDestination destination)
        {
            Opcode = opcode;
            Destination = destination;
        }

        public ushort Opcode { get; }
        public PacketDestination Destination { get; }

        public void Invoke(Packet packet)
        {
        }
    }

    private sealed class TestHook : IPacketHook
    {
        public TestHook(ushort opcode, PacketDestination destination)
        {
            Opcode = opcode;
            Destination = destination;
        }

        public ushort Opcode { get; }
        public PacketDestination Destination { get; }

        public Packet ReplacePacket(Packet packet)
        {
            return packet;
        }
    }
}
