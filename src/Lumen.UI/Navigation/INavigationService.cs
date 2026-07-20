namespace Lumen.UI.Navigation;

/// <summary>Lets a page request navigation to another page by id (e.g. Home's Launch button).</summary>
public interface INavigationService
{
    event Action<string>? NavigationRequested;

    void NavigateTo(string pageId);
}
