using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using vatSysToolbox.Controls;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin.FlightPlan;

/// <summary>
/// A Flight Plan Window with the layout of the Eurocat window. <see cref="FlightPlanWindow"/> shows it in a vatSys
/// window, with the same title bar as the other vatSys windows.
/// </summary>
/// <remarks>
/// The view model has the state and the actions. This class gives the keyboard focus to the fields, connects the
/// scroll bars of the multi-line fields, and sends Enter and Escape to the view model.
/// </remarks>
public partial class FlightPlanView : UserControl
{
    const uint RedrawInvalidate = 0x0001;
    const uint RedrawAllChildren = 0x0080;
    const uint RedrawUpdateNow = 0x0100;

    readonly FlightPlanViewModel _viewModel;
    readonly Dictionary<FieldViewModel, TextBox> _fieldBoxes = new();
    bool _fieldsConnected;

    /// <param name="viewModel">The state and the actions of the window.</param>
    /// <param name="windowHandle">The window that shows the view.</param>
    public FlightPlanView(FlightPlanViewModel viewModel, IWindowHandle windowHandle)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        FitRowsToButtonGrid();

        Loaded += (_, _) => ConnectScrollBars();

        // WPF controls in the window can get the keyboard focus only after the window shows
        windowHandle.Shown += (_, _) => FocusField(_viewModel.Acid);
    }

    [DllImport("user32.dll")]
    static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    /// <summary>
    /// Lays out and paints the window right now. Call this method after the window shows.
    /// </summary>
    /// <remarks>
    /// Windows paints a window only when the message queue of its thread is empty, and WPF renders from the
    /// dispatcher queue. The vatSys UI thread is rarely idle, thus a window that waits for the queue fills in piece by
    /// piece. This method does the layout, and then asks Windows to paint the window and all its children at once.
    /// </remarks>
    internal void FlushPaint()
    {
        UpdateLayout();

        if (PresentationSource.FromVisual(this) is not HwndSource source)
            return;

        // Paint the vatSys window too, so that its title bar and border show with the content
        var host = System.Windows.Forms.Control.FromChildHandle(source.Handle);
        var window = host?.FindForm()?.Handle ?? source.Handle;
        RedrawWindow(window, IntPtr.Zero, IntPtr.Zero, RedrawInvalidate | RedrawAllChildren | RedrawUpdateNow);
    }

    // The window measures the view with no width limit. A row that wraps text would then take the width of its text,
    // and make the window wider. Give those rows the width of the button grid, which is the widest row. One
    // measurement here saves a second layout pass and a second resize of the window.
    void FitRowsToButtonGrid()
    {
        ButtonGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = ButtonGrid.DesiredSize.Width;

        RouteRow.Width = width;
        RemarksRow.Width = width;
        MessageArea.Width = width;
    }

    // The text boxes bind to the fields through their DataContext. Find the text box of each field, so that the field
    // can ask for the focus.
    void ConnectFields()
    {
        if (_fieldsConnected)
            return;

        _fieldsConnected = true;

        foreach (var box in FindTextBoxes(this))
        {
            if (box.DataContext is not FieldViewModel field || _fieldBoxes.ContainsKey(field))
                continue;

            _fieldBoxes[field] = box;
            field.FocusRequested += (_, _) => FocusField(field);
            field.SelectAllRequested += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(box.SelectAll));
            field.PropertyChanged += (_, e) => OnFieldPropertyChanged(field, box, e);
        }
    }

    // When a field stops being editable while the user types in it, the focus goes to the view. Then Enter and Escape
    // still work.
    void OnFieldPropertyChanged(FieldViewModel field, TextBox box, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FieldViewModel.IsEditable) && !field.IsEditable && box.IsKeyboardFocusWithin)
            Focus();
    }

    // The field must be fully shown before the text box can get the keyboard focus. Thus, focus the text box after
    // the layout has finished.
    void FocusField(FieldViewModel field)
    {
        ConnectFields();
        if (!_fieldBoxes.TryGetValue(field, out var box))
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (field.IsEditable)
                Keyboard.Focus(box);
        }));
    }

    // The scroll bar controls the scroll viewer in the template of the text box
    void ConnectScrollBars()
    {
        ConnectFields();
        ConnectScrollBar(RteBox, RteScrollBar);
        ConnectScrollBar(OtherBox, OtherScrollBar);
    }

    static void ConnectScrollBar(TextBox box, CustomScrollBar scrollBar)
    {
        box.ApplyTemplate();
        if (box.Template.FindName("PART_ContentHost", box) is ScrollViewer scrollViewer)
            scrollBar.ScrollViewer = scrollViewer;
    }

    static IEnumerable<TextBox> FindTextBoxes(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is not DependencyObject dependencyObject)
                continue;

            if (dependencyObject is TextBox box)
                yield return box;

            foreach (var descendant in FindTextBoxes(dependencyObject))
                yield return descendant;
        }
    }

    // The buttons never take the focus. Thus, Enter and Escape come here from the fields and from the view.
    void View_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (_viewModel.EnterCommand.CanExecute(null))
                    _viewModel.EnterCommand.Execute(null);

                e.Handled = true;
                break;

            case Key.Escape:
                _viewModel.EscapeCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
