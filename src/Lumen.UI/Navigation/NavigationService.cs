namespace Lumen.UI.Navigation;

/// <inheritdoc />
public sealed class NavigationService : INavigationService
{
    public event Action<string>? NavigationRequested;

    public void NavigateTo(string pageId) => NavigationRequested?.Invoke(pageId);
}
