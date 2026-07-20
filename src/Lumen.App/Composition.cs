using Lumen.Composition;
using Lumen.Core.Abstractions;
using Lumen.UI.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.App;

/// <summary>
/// The Avalonia head's composition root: the shared service graph plus this head's
/// <see cref="IThemeApplier"/> implementation. The WinUI head builds an identical provider,
/// substituting its own theme applier.
/// </summary>
internal static class Composition
{
    public static IServiceProvider Build() =>
        new ServiceCollection()
            .AddLumenServices()
            .AddSingleton<IThemeApplier, AvaloniaThemeApplier>()
            .BuildServiceProvider();
}
