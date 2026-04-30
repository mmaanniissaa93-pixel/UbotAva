using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using UBot.Core.Bootstrap;
using UBot.Avalonia.Services;

namespace UBot.Avalonia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ServiceProviderFactory.CreateServices();
            IUbotCoreService core = new UbotCoreService();
            var state = new AppState();

            MainWindow.CoreService = core;
            MainWindow.State       = state;

            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
