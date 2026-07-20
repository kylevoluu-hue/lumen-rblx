using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Lumen.Core.Abstractions;
using Lumen.Core.Common;
using Lumen.Core.Configuration;
using Lumen.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = Composition.Build();

            // Prepare local storage and load persisted state before the first view binds.
            services.GetRequiredService<ILumenPaths>().EnsureCreated();
            var settings = services.GetRequiredService<ISettingsService>();
            settings.LoadAsync().GetAwaiter().GetResult();
            services.GetRequiredService<IAccountManager>().LoadAsync().GetAwaiter().GetResult();
            services.GetRequiredService<IProfileService>().LoadAsync().GetAwaiter().GetResult();

            // Apply the saved accent colour before the first window renders.
            services.GetRequiredService<Lumen.UI.Theming.IThemeApplier>().ApplyAccent(settings.Current.Appearance.AccentColor);

            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<ShellViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
