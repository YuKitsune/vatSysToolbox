using System.Windows;
using System.Windows.Media;
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

    // After the first time, the window manager opens the window with its last size
    bool _hasOpened;

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
        var isFirstOpen = !_hasOpened;
        _hasOpened = true;

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
            configureForm: form => SetSizeOnLoad(form, view!, isFirstOpen));
    }

    // The size of the strips and the size of the vatSys border are correct only after the view is in the window
    void SetSizeOnLoad(VatSysForm form, TagList view, bool isFirstOpen)
    {
        view.Loaded += OnLoaded;

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            view.Loaded -= OnLoaded;

            try
            {
                SetSize(form, view, isFirstOpen);
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, "Cannot set the size of the Tag List");
            }
        }
    }

    // The window starts with the size of all the strips. It cannot become smaller than one strip.
    static void SetSize(VatSysForm form, TagList view, bool isFirstOpen)
    {
        var contentSize = view.MeasureContent();
        var minimumContentSize = view.MeasureOneStrip();

        // The measures above have no size limit. Thus, make the parent measure the view again in the size of the window.
        // The view itself would measure again with no size limit.
        (VisualTreeHelper.GetParent(view) as UIElement)?.InvalidateMeasure();

        // vatSys draws its own border and title bar. Thus, use the real size of the border and title bar.
        var borderWidth = form.Width - form.ClientSize.Width;
        var borderHeight = form.Height - form.ClientSize.Height;

        form.MinimumSize = new System.Drawing.Size(
            (int)Math.Ceiling(minimumContentSize.Width) + borderWidth,
            (int)Math.Ceiling(minimumContentSize.Height) + borderHeight);

        if (!isFirstOpen)
            return;

        form.Size = new System.Drawing.Size(
            (int)Math.Ceiling(contentSize.Width) + borderWidth,
            (int)Math.Ceiling(contentSize.Height) + borderHeight);
    }
}
