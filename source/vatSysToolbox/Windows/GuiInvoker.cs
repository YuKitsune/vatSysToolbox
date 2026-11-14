using System.Windows.Forms;

namespace vatSysToolbox.Windows;

/// <summary>
/// Does an action on the vatSys UI thread. Do not use this interface unless you must.
/// </summary>
/// <remarks>
/// <para>
/// You must use this interface only if both of these conditions are true:
/// </para>
/// <list type="number">
/// <item>
/// The code can run on a thread that is not the vatSys UI thread. For example: <c>OnFDRUpdate</c> and
/// <c>OnRadarTrackUpdate</c>, the <c>Network</c> events, the plugin messenger handlers, the MediatR handlers, the
/// <see cref="WorkQueue"/>, <c>Task.Run</c>, <c>System.Timers.Timer</c>, and code after
/// <c>ConfigureAwait(false)</c>. Some vatSys events (for example, <c>MMI.SelectedTrackChanged</c>) can occur on any
/// thread.
/// </item>
/// <item>
/// The code changes something that must change on the UI thread:
/// <list type="bullet">
/// <item>A WinForms form or control, or a WPF element. For example, make, show, or close a window.</item>
/// <item>The state that a WPF view binds to. For example, a view model property or collection.</item>
/// <item>A vatSys menu item, or something that is only for the UI thread, such as a message filter.</item>
/// </list>
/// </item>
/// </list>
/// <para>
/// Do not use this interface in these cases:
/// </para>
/// <list type="bullet">
/// <item>
/// The code always runs on the UI thread. For example: WinForms and WPF event handlers, menu item clicks, form events,
/// <c>Application.Idle</c>, and a <c>DispatcherTimer</c>. On the UI thread, the action runs immediately, thus the call
/// does nothing. Call the code directly, and catch the exceptions, because an exception that is not caught on the UI
/// thread stops vatSys.
/// </item>
/// <item>
/// The code does not change the UI. For example, it reads or changes plugin data, or it sends a message.
/// </item>
/// <item>
/// A method that you call already goes to the UI thread. For example, <see cref="WindowManager"/> opens and closes
/// windows on the UI thread.
/// </item>
/// <item>
/// The UI does not change. Do the checks on the background thread first. For example, if a window shows one aircraft,
/// do not post the updates of the other aircraft.
/// </item>
/// </list>
/// <para>
/// On a thread that is not the UI thread, each call posts one message to the UI thread. vatSys and all the plugins use
/// the same UI thread, and a thread can have a maximum of 10,000 posted messages. If the queue becomes full, the WPF
/// content in vatSys can stop. Thus, do not call this method for each radar track update or each flight plan update.
/// </para>
/// </remarks>
public interface IGuiInvoker
{
    /// <summary>
    /// Does the action on the vatSys UI thread, and reports its exceptions. On the UI thread, the action runs
    /// immediately. On another thread, the action runs later. See <see cref="IGuiInvoker"/> for when you must use this
    /// method.
    /// </summary>
    /// <param name="action">The action. It gets the vatSys main form.</param>
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
