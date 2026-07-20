namespace Lumen.UI.ViewModels.Pages;

/// <summary>
/// An honest scaffold for a navigation destination whose full functionality lands in a later
/// phase. It never pretends the feature works; it states what the page will do and which phase
/// implements it. The service interfaces these pages will bind to are already defined.
/// </summary>
public sealed class PlaceholderPageViewModel : PageViewModel
{
    public PlaceholderPageViewModel(string title, string description, string roadmapNote)
    {
        Title = title;
        Description = description;
        RoadmapNote = roadmapNote;
    }

    public override string Title { get; }

    public override string Description { get; }

    public string RoadmapNote { get; }
}
