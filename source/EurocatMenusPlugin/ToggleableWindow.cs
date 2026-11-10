using System.Windows.Forms;
using CommunityToolkit.Mvvm.Messaging;

namespace EurocatMenusPlugin;

/// <summary>
/// A window of vatSys or of another plugin that a button opens and closes.
/// Used to hook into opened and closed events.
/// </summary>
/// <remarks>
/// The window events come from <see cref="VatSysPatches"/>. Use this class on the UI thread. The messenger keeps only a
/// weak reference to this class.
/// </remarks>
public class ToggleableWindow
{
    readonly Func<Form, bool> _isWindow;
    readonly Action _open;

    /// <param name="isWindow">Gives <c>true</c> for the window.</param>
    /// <param name="open">Opens the window.</param>
    /// <param name="vatSysMessenger">Sends the vatSys events.</param>
    public ToggleableWindow(Func<Form, bool> isWindow, Action open, IMessenger vatSysMessenger)
    {
        _isWindow = isWindow;
        _open = open;

        // vatSys hides some windows, and does not close them
        vatSysMessenger.Register<ToggleableWindow, WindowChangedMessage>(
            this,
            (window, _) => window.SetIsOpen(window.FindWindow(closingWindow: null) is not null));
        vatSysMessenger.Register<ToggleableWindow, WindowClosedMessage>(
            this,
            (window, message) => window.SetIsOpen(window.FindWindow(message.Window) is not null));
    }

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
            return;
        }

        _open();

        // Some plugins open their window later (for example, the CPDLC plugin uses a work queue). Thus, show the window
        // as open now. If the window does not open, the next window event shows it as closed.
        SetIsOpen(true);
    }

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
        return Application.OpenForms
            .Cast<Form>()
            .FirstOrDefault(form => form != closingWindow && form.Visible && _isWindow(form));
    }
}
