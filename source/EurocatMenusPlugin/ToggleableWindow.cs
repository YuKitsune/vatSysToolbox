using System.Windows.Forms;
using vatSysToolbox;

namespace EurocatMenusPlugin;

/// <summary>
/// A window of vatSys or of another plugin that a button opens and closes.
/// Used to hook into opened and closed events.
/// </summary>
/// <remarks>
/// vatSys does not send events when its windows open. Thus, call <see cref="Refresh"/> at an interval. Use this class
/// on the UI thread.
/// </remarks>
/// <param name="isWindow">Gives <c>true</c> for the window.</param>
/// <param name="open">Opens the window.</param>
public class ToggleableWindow(Func<Form, bool> isWindow, Action open, IErrorReporter errorReporter)
{
    // The open windows that this class listens to, so that IsOpen changes as soon as the window closes
    readonly HashSet<Form> _watchedWindows = [];

    public bool IsOpen { get; private set; }

    public event EventHandler? IsOpenChanged;

    /// <summary>
    /// Closes the window if it is open. Otherwise, opens it.
    /// </summary>
    public void Toggle()
    {
        if (FindWindow(closingWindow: null) is { } window)
        {
            window.Close();
            Refresh();
            return;
        }

        open();
        Refresh();

        // Some plugins open their window later (for example, the CPDLC plugin uses a work queue). Thus, show the window
        // as open now. If the window does not open, the next refresh shows it as closed.
        SetIsOpen(true);
    }

    public void Refresh() => SetIsOpen(FindWindow(closingWindow: null) is not null);

    void SetIsOpen(bool isOpen)
    {
        if (IsOpen == isOpen)
            return;

        IsOpen = isOpen;
        IsOpenChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <param name="closingWindow">
    /// A window that is closing. It can still be visible, but it must not be found.
    /// </param>
    Form? FindWindow(Form? closingWindow)
    {
        var window = Application.OpenForms
            .Cast<Form>()
            .FirstOrDefault(form => form != closingWindow && form.Visible && isWindow(form));

        if (window is not null && _watchedWindows.Add(window))
        {
            window.VisibleChanged += Window_VisibleChanged;
            window.FormClosed += Window_FormClosed;
        }

        return window;
    }

    // vatSys hides some windows, and does not close them
    void Window_VisibleChanged(object sender, EventArgs e) => Run(Refresh);

    void Window_FormClosed(object sender, FormClosedEventArgs e)
    {
        var window = (Form)sender;
        window.VisibleChanged -= Window_VisibleChanged;
        window.FormClosed -= Window_FormClosed;
        _watchedWindows.Remove(window);

        Run(() => SetIsOpen(FindWindow(window) is not null));
    }

    // An exception that is not caught on the UI thread stops vatSys.
    void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex);
        }
    }
}
