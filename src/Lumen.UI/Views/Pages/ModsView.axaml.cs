using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Lumen.UI.ViewModels.Pages;

namespace Lumen.UI.Views.Pages;

public partial class ModsView : UserControl
{
    public ModsView() => InitializeComponent();

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ModsViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a .lumenmod package",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Lumen mod package") { Patterns = new[] { "*.lumenmod", "*.zip" } },
            },
        });

        var picked = files.Count > 0 ? files[0] : null;
        var path = picked?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            await viewModel.ImportAndApplyAsync(path);
        }
    }
}
