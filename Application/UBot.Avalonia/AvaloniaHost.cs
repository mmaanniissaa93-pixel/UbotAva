using System;
using Avalonia;

namespace UBot.Avalonia;

public static class AvaloniaHost
{
    [STAThread]
    public static void Run(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
