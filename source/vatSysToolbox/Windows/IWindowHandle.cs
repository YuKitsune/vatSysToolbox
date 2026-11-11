using System.Windows.Forms;

namespace vatSysToolbox.Windows;

public interface IWindowHandle
{
    /// <summary>
    /// Occurs when the window shows for the first time. WPF controls in the window can get the keyboard focus only
    /// after this event.
    /// </summary>
    event EventHandler? Shown;
    event EventHandler? Closed;
    event EventHandler? Moved;

    void Focus();
    void Close();
}

public class WindowHandle : IWindowHandle
{
    readonly VatSysForm _form;

    public WindowHandle(VatSysForm form)
    {
        _form = form;
        _form.Shown += (_, _) => Shown?.Invoke(this, EventArgs.Empty);
        _form.FormClosed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
        _form.Move += (_, _) => Moved?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Shown;
    public event EventHandler? Closed;
    public event EventHandler? Moved;

    public void Focus()
    {
        _form.Show();
        _form.WindowState = FormWindowState.Normal;
        _form.Activate();
    }

    public void Close()
    {
        _form.ForceClose();
    }
}
