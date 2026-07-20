using Lumen.Composition;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.UI.Theming;
using Lumen.UI.ViewModels;
using Lumen.WinUI.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Lumen.WinUI;

/// <summary>
/// WinUI 3 application entry point. Builds the SAME service graph as the Avalonia head via
/// <see cref="LumenServiceRegistration.AddLumenServices"/>, substituting the WinUI theme applier,
/// then shows the shared <see cref="ShellViewModel"/> in a WinUI window.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection()
            .AddLumenServices()
            .AddSingleton<IThemeApplier, WinUIThemeApplier>()
            .BuildServiceProvider();

        services.GetRequiredService<ILumenPaths>().EnsureCreated();
        var settings = services.GetRequiredService<ISettingsService>();
        settings.LoadAsync().GetAwaiter().GetResult();
        services.GetRequiredService<IAccountManager>().LoadAsync().GetAwaiter().GetResult();
        services.GetRequiredService<IProfileService>().LoadAsync().GetAwaiter().GetResult();
        services.GetRequiredService<IThemeApplier>().ApplyAccent(settings.Current.Appearance.AccentColor);

        _window = new MainWindow(services.GetRequiredService<ShellViewModel>());
        _window.Activate();
    }
}
