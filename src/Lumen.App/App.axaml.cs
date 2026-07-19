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
            services.GetRequiredService<ISettingsService>().LoadAsync().GetAwaiter().GetResult();
            services.GetRequiredService<IAccountManager>().LoadAsync().GetAwaiter().GetResult();
            services.GetRequiredService<IProfileService>().LoadAsync().GetAwaiter().GetResult();

            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<ShellViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
