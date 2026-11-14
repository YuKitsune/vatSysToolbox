using System.Windows;
using System.Windows.Forms;
using vatsys;

namespace vatSysToolbox.Windows;

/// <summary>
/// Opens and closes <see cref="VatSysForm"/> windows. A key identifies each window, so that only one window with
/// the same key is open at a time. The window manager keeps the last location and size of each window, and opens the
/// window at that location again. A window that sizes to its content gets its size from the content, and not the last
/// size.
/// </summary>
public class WindowManager(IGuiInvoker guiInvoker)
{
    readonly Dictionary<string, VatSysForm> _forms = new();
    readonly Dictionary<string, System.Drawing.Rectangle> _lastBounds = new();
    readonly object _gate = new();

    /// <summary>
    /// Opens a window. If a window with the same key is open, this method focuses that window.
    /// </summary>
    public void FocusOrCreateWindow(
        string key,
        string title,
        Func<IWindowHandle, FrameworkElement> createView,
        bool sizeToContent = true,
        bool canClose = true,
        bool canMinimise = true,
        Size? size = null,
        Action<VatSysForm>? configureForm = null)
    {
        guiInvoker.InvokeOnGUI(mainForm =>
        {
            lock (_gate)
            {
                if (_forms.TryGetValue(key, out var existingForm))
                {
                    existingForm.WindowHandle.Focus();
                    return;
                }

                CreateWindow(mainForm, key, title, createView, sizeToContent, canClose, canMinimise, size, configureForm);
            }
        });
    }

    /// <summary>
    /// Opens a window. If a window with the same key is open, this method does nothing.
    /// </summary>
    public void TryCreateWindow(
        string key,
        string title,
        Func<IWindowHandle, FrameworkElement> createView,
        bool sizeToContent = true,
        bool canClose = true,
        bool canMinimise = true,
        Size? size = null,
        Action<VatSysForm>? configureForm = null)
    {
        // Callers can call this method often (for example, each time the data changes). Do not post to the UI thread
        // if the window is open.
        lock (_gate)
        {
            if (_forms.ContainsKey(key))
                return;
        }

        guiInvoker.InvokeOnGUI(mainForm =>
        {
            lock (_gate)
            {
                if (_forms.ContainsKey(key))
                    return;

                CreateWindow(mainForm, key, title, createView, sizeToContent, canClose, canMinimise, size, configureForm);
            }
        });
    }

    /// <summary>
    /// Opens a <see cref="PopupForm"/> at the mouse cursor.
    /// </summary>
    public void ShowPopup(Func<IWindowHandle, FrameworkElement> createView)
    {
        guiInvoker.InvokeOnGUI(mainForm =>
        {
            var form = new PopupForm(createView);
            MMI.EnsureWindowVisible(form);
            form.Show(mainForm);
        });
    }

    public bool TryGetWindow(string key, out IWindowHandle? windowHandle)
    {
        lock (_gate)
        {
            if (_forms.TryGetValue(key, out var form))
            {
                windowHandle = form.WindowHandle;
                return true;
            }

            windowHandle = null;
            return false;
        }
    }

    public void CloseWindow(string key)
    {
        // Do not post to the UI thread if the window is not open
        lock (_gate)
        {
            if (!_forms.ContainsKey(key))
                return;
        }

        guiInvoker.InvokeOnGUI(_ =>
        {
            VatSysForm? form;
            lock (_gate)
            {
                if (!_forms.TryGetValue(key, out form))
                    return;
            }

            try
            {
                form.ForceClose();
            }
            catch (ObjectDisposedException)
            {
                // Form may have already been closed or disposed
            }
        });
    }

    void CreateWindow(
        Form mainForm,
        string key,
        string title,
        Func<IWindowHandle, FrameworkElement> createView,
        bool sizeToContent,
        bool canClose,
        bool canMinimise,
        Size? size,
        Action<VatSysForm>? configureForm)
    {
        var form = new VatSysForm(title, createView, sizeToContent, canClose, canMinimise)
        {
            StartPosition = FormStartPosition.CenterParent
        };

        // If the view closes the form before it has a handle, the form is disposed and cannot be shown.
        if (form.IsDisposed)
            return;

        if (size.HasValue)
        {
            form.Width = (int)size.Value.Width;
            form.Height = (int)size.Value.Height;
        }

        if (_lastBounds.TryGetValue(key, out var lastBounds))
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = lastBounds.Location;

            if (!sizeToContent)
                form.Size = lastBounds.Size;
        }

        configureForm?.Invoke(form);

        form.PerformLayout();
        form.Closed += (_, _) => OnWindowClosed(key, form);

        _forms[key] = form;
        form.Show(mainForm);
    }

    void OnWindowClosed(string key, VatSysForm form)
    {
        lock (_gate)
        {
            // If the window is minimised or maximised, Bounds is not the normal position and size of the window
            _lastBounds[key] = form.WindowState == FormWindowState.Normal
                ? form.Bounds
                : form.RestoreBounds;

            _forms.Remove(key);
        }
    }
}
