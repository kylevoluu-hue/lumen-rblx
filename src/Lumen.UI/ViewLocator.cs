using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Lumen.UI.ViewModels;

namespace Lumen.UI;

/// <summary>
/// Resolves a view for a view-model by naming convention: <c>...ViewModels.Pages.HomeViewModel</c>
/// maps to <c>...Views.Pages.HomeView</c>. Registered as an application data template.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "No content." };
        }

        var viewName = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var viewType = Type.GetType(viewName);

        return viewType is not null
            ? (Control)Activator.CreateInstance(viewType)!
            : new TextBlock { Text = $"View not found: {viewName}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
