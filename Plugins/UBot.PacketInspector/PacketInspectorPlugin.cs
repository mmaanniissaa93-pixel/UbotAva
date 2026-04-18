using System;
using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;

namespace UBot.PacketInspector;

public class PacketInspectorPlugin : IPlugin
{
    public string Author => "UBot Team";
    public string Description => "Inspects incoming/outgoing packets with live filters and export support.";
    public string Name => "UBot.PacketInspector";
    public string Title => "Packet Inspector";
    public string Version => "0.1.0";
    public bool Enabled { get; set; }
    public bool DisplayAsTab => false;
    public int Index => 110;
    public bool RequireIngame => false;

    public void Initialize()
    {
        PacketCaptureStore.CaptureEnabled = GlobalConfig.Get("UBot.PacketInspector.CaptureEnabled", false);
        PacketCaptureStore.MaxEntries = Math.Clamp(GlobalConfig.Get("UBot.PacketInspector.MaxRows", 2000), 100, 10000);
    }

    public Control View => Views.View.Instance;

    public void Translate()
    {
        LanguageManager.Translate(View, Kernel.Language);
    }

    public void OnLoadCharacter()
    {
        // do nothing
    }

    public void Enable()
    {
        PacketCaptureStore.CaptureEnabled = GlobalConfig.Get("UBot.PacketInspector.CaptureEnabled", false);

        if (View != null)
            View.Enabled = true;
    }

    public void Disable()
    {
        PacketCaptureStore.CaptureEnabled = false;

        if (View != null)
            View.Enabled = false;
    }
}

