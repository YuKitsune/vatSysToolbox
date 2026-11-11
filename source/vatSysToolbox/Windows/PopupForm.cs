using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace vatSysToolbox.Windows;

/// <summary>
/// A form with no border or title bar, at the mouse cursor. The form closes when it is not the active form (for
/// example, when the user clicks outside it).
/// </summary>
public class PopupForm : Form
{
    bool _isClosing;

    public IWindowHandle WindowHandle { get; }

    public PopupForm(Func<IWindowHandle, FrameworkElement> createView)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = Cursor.Position;

        WindowHandle = new PopupWindowHandle(this);
        var view = createView(WindowHandle);

        // A Canvas measures its child with no size limit. The view then always has its natural size, and SizeChanged
        // occurs when WPF has done the layout of the view.
        var canvas = new Canvas();
        canvas.Children.Add(view);

        var elementHost = new ElementHost
        {
            Dock = DockStyle.Fill,
            Child = canvas
        };
        Controls.Add(elementHost);

        view.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        ResizeToContent(view.DesiredSize);
        view.SizeChanged += (_, e) => ResizeToContent(e.NewSize);
    }

    void ResizeToContent(System.Windows.Size contentSize)
    {
        ClientSize = new System.Drawing.Size(
            (int)Math.Ceiling(contentSize.Width),
            (int)Math.Ceiling(contentSize.Height));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        _isClosing = !e.Cancel;
    }

    // The form also deactivates while it closes. Do not close it a second time.
    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);

        if (!_isClosing && !IsDisposed)
            Close();
    }

    class PopupWindowHandle : IWindowHandle
    {
        readonly PopupForm _form;

        public PopupWindowHandle(PopupForm form)
        {
            _form = form;
            _form.Shown += (_, _) => Shown?.Invoke(this, EventArgs.Empty);
            _form.FormClosed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
            _form.Move += (_, _) => Moved?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Shown;
        public event EventHandler? Closed;
        public event EventHandler? Moved;

        public void Focus() => _form.Activate();

        // The popup can close before the view model calls this method (for example, when the user clicks outside it)
        public void Close()
        {
            if (!_form._isClosing && !_form.IsDisposed)
                _form.Close();
        }
    }
}
