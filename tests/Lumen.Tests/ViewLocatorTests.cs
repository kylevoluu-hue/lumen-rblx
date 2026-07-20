using Lumen.UI;
using Lumen.UI.ViewModels;
using Xunit;

namespace Lumen.Tests;

public class ViewLocatorTests
{
    /// <summary>
    /// Every navigable page view-model (in the shared presentation assembly) must resolve to a real
    /// view in the Avalonia UI assembly under the ViewLocator's naming convention
    /// (…ViewModels.Pages.XViewModel → …Views.Pages.XView). This reflection guard catches naming
    /// mismatches that would otherwise only surface at runtime as a "View not found" fallback.
    /// </summary>
    [Fact]
    public void Every_page_view_model_resolves_to_a_real_view()
    {
        var viewModelAssembly = typeof(PageViewModel).Assembly;      // Lumen.Presentation
        var viewAssembly = typeof(ViewLocator).Assembly;             // Lumen.UI (Avalonia head)

        var pageViewModels = viewModelAssembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && typeof(PageViewModel).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(pageViewModels);

        var missing = new List<string>();
        foreach (var vmType in pageViewModels)
        {
            var viewName = vmType.FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            if (viewAssembly.GetType(viewName) is null)
            {
                missing.Add($"{vmType.Name} -> {viewName}");
            }
        }

        Assert.True(missing.Count == 0, "Page view-models without a matching view: " + string.Join(", ", missing));
    }
}
