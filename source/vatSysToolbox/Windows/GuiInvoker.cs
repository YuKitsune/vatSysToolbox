using System.Windows.Forms;

namespace vatSysToolbox.Windows;

public interface IGuiInvoker
{
    void InvokeOnGUI(Action<Form> action);
}

public class GuiInvoker(IErrorReporter errorReporter) : IGuiInvoker
{
    const string VatSysMainFormKey = "MainForm";

    public void InvokeOnGUI(Action<Form> action)
    {
        var mainForm = Application.OpenForms[VatSysMainFormKey];
        if (mainForm == null)
            return;

        // Check if the window handle is valid before invoking
        if (mainForm.IsDisposed || !mainForm.IsHandleCreated)
            return;

        // If already on UI thread, execute directly to avoid deadlock
        if (!mainForm.InvokeRequired)
        {
            Run(action, mainForm);
            return;
        }

        try
        {
            vatsys.MMI.InvokeOnGUI(delegate { Run(action, mainForm); });
        }
        catch (ObjectDisposedException)
        {
            // Control was disposed during invocation - ignore during shutdown
        }
        catch (InvalidOperationException)
        {
            // Window handle was destroyed during invocation - ignore during shutdown
        }
    }

    // An exception that is not caught on the UI thread stops vatSys.
    void Run(Action<Form> action, Form mainForm)
    {
        try
        {
            action(mainForm);
        }
        catch (Exception ex)
        {
            try
            {
                errorReporter.ReportError(ex);
            }
            catch
            {
                // Ignore errors during error reporting
            }
        }
    }
}
