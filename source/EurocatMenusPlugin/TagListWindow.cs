using System.Windows;
using EurocatMenusPlugin.ViewModels;
using EurocatMenusPlugin.Views;
using vatsys;
using vatSysToolbox;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin;

/// <summary>
/// Opens the Tag List when one or more quick tags exist, and closes it when no quick tags exist.
/// </summary>
/// <remarks>
/// vatSys does not send events when the quick tags change. Thus, call <see cref="Refresh"/> at an interval.
/// </remarks>
public class TagListWindow(WindowManager windowManager, IErrorReporter errorReporter)
{
    const string WindowKey = "TagList";
    const string WindowTitle = "Tag List";
    static readonly Size WindowSize = new(480, 150);

    // The view model of the open window, or null if the window is closed
    TagListViewModel? _viewModel;

    /// <summary>
    /// Reads the quick tags again, and opens or closes the window.
    /// </summary>
    public void Refresh()
    {
        var quickTags = RDP.QuickTags;

        if (_viewModel is null)
        {
            if (quickTags.Count > 0)
                Open(quickTags);

            return;
        }

        if (quickTags.Count == 0)
        {
            windowManager.CloseWindow(WindowKey);
            return;
        }

        _viewModel.Refresh(quickTags);
    }

    void Open(IList<QuickTag> quickTags)
    {
        windowManager.TryCreateWindow(
            WindowKey,
            WindowTitle,
            windowHandle =>
            {
                var viewModel = new TagListViewModel(errorReporter);
                viewModel.Refresh(quickTags);

                _viewModel = viewModel;
                windowHandle.Closed += (_, _) => _viewModel = null;

                return new TagList(viewModel);
            },
            sizeToContent: false,
            canClose: false,
            canMinimise: false,
            size: WindowSize);
    }
}
