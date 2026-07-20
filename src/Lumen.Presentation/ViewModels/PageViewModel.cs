namespace Lumen.UI.ViewModels;

/// <summary>Base type for a navigable page. Pages load their data in <see cref="InitializeAsync"/>.</summary>
public abstract class PageViewModel : ViewModelBase
{
    public abstract string Title { get; }

    public virtual string Description => string.Empty;

    /// <summary>
    /// Called when the page becomes active. Implementations must handle their own errors and never
    /// throw — surface problems as state, not exceptions.
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;
}
