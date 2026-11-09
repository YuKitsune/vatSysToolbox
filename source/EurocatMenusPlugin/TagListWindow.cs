using System.Windows;
using EurocatMenusPlugin.ViewModels;
using EurocatMenusPlugin.Views;
using vatsys;
using vatSysToolbox;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin;

/// <summary>
/// Shows the Tag List while it is turned on and one or more quick tags exist.
/// </summary>
/// <remarks>
/// vatSys does not send events when the quick tags change. Thus, call <see cref="Refresh"/> at an interval.
/// </remarks>
public class TagListWindow(WindowManager windowManager, IErrorReporter errorReporter)
{
    const string WindowKey = "TagList";
    const string WindowTitle = "Tag List";

    // The view model of the open window, or null if the window is closed
    TagListViewModel? _viewModel;

    /// <summary>
    /// <c>true</c> if the Tag List shows when quick tags exist.
    /// </summary>
    public bool IsOn { get; private set; }

    public void Toggle()
    {
        IsOn = !IsOn;
        Refresh();
    }

    /// <summary>
    /// Reads the quick tags again, and opens or closes the window.
    /// </summary>
    public void Refresh()
    {
        var quickTags = RDP.QuickTags;

        if (!IsOn || quickTags.Count == 0)
        {
            if (_viewModel is not null)
                windowManager.CloseWindow(WindowKey);

            return;
        }

        if (_viewModel is null)
            Open(quickTags);
        else
            _viewModel.Refresh(quickTags);
    }

    void Open(IList<QuickTag> quickTags)
    {
        TagList? view = null;

        windowManager.TryCreateWindow(
            WindowKey,
            WindowTitle,
            windowHandle =>
            {
                var viewModel = new TagListViewModel(errorReporter);
                viewModel.Refresh(quickTags);

                _viewModel = viewModel;
                windowHandle.Closed += (_, _) => _viewModel = null;

                view = new TagList(viewModel);
                return view;
            },
            sizeToContent: false,
            canClose: false,
            canMinimise: false,
            configureForm: form => SetSize(form, view!));
    }

    // The window starts with the size of all the strips. It cannot become smaller than one strip.
    static void SetSize(VatSysForm form, TagList view)
    {
        var contentSize = view.MeasureContent();
        var minimumContentSize = view.MeasureOneStrip();

        // vatSys draws its own border and title bar. Thus, use the real size of the border and title bar.
        var borderWidth = form.Width - form.ClientSize.Width;
        var borderHeight = form.Height - form.ClientSize.Height;

        form.MinimumSize = new System.Drawing.Size(
            (int)Math.Ceiling(minimumContentSize.Width) + borderWidth,
            (int)Math.Ceiling(minimumContentSize.Height) + borderHeight);

        form.Size = new System.Drawing.Size(
            (int)Math.Ceiling(contentSize.Width) + borderWidth,
            (int)Math.Ceiling(contentSize.Height) + borderHeight);
    }
}
