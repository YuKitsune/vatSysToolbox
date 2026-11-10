using vatSysToolbox;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin;

/// <summary>
/// Opens and closes the vatSys Strip Setup window, and sends the events when it opens and closes.
/// </summary>
/// <remarks>
/// vatSys does not send events when the windows open. Thus, call <see cref="Refresh"/> at an interval. Make this class
/// before the messenger starts, because it subscribes to its message.
/// </remarks>
public class StripSetupWindow
{
    // The message. The payload is not used.
    const string ToggleMessage = "dev.eoinmotherway.eurocatmenus.toggle-strip-setup-window";

    // The events
    const string OpenedEvent = "dev.eoinmotherway.eurocatmenus.strip-setup-window-opened";
    const string ClosedEvent = "dev.eoinmotherway.eurocatmenus.strip-setup-window-closed";

    // The vatSys menu item and window
    const string StripSetupItem = "setupToolStripMenuItem2";
    const string WindowTypeName = "StripSetupWindow";

    readonly ToggleableWindow _window;

    public StripSetupWindow(
        VatSysMenu vatSysMenu,
        MenuEvents events,
        IPluginMessenger messenger,
        IGuiInvoker guiInvoker,
        IErrorReporter errorReporter)
    {
        _window = new ToggleableWindow(
            window => window.GetType().Name == WindowTypeName,
            () => vatSysMenu.Click(StripSetupItem),
            errorReporter);
        _window.IsOpenChanged += (_, _) => events.Raise(_window.IsOpen ? OpenedEvent : ClosedEvent);

        // The messenger receives the messages on a background thread
        messenger.Subscribe(ToggleMessage, (_, _) =>
        {
            guiInvoker.InvokeOnGUI(_ => _window.Toggle());
            return Task.CompletedTask;
        });
    }

    public void Refresh()
    {
        _window.Refresh();
    }
}
