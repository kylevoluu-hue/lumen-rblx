using Lumen.UI.ViewModels;

namespace Lumen.UI.Navigation;

/// <summary>A single navigation destination: its id, label, icon glyph, and page view-model.</summary>
public sealed record NavigationEntry(string Id, string Title, string Glyph, PageViewModel Page);
