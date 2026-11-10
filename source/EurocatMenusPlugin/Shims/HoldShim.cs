using CommunityToolkit.Mvvm.Messaging;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin.Shims;

/// <summary>
/// Handles the messages of the Hold plugin, and sends its events, until the Hold plugin does this itself. The shim
/// opens and closes the Hold Setup window with the Hold plugin menu item.
/// </summary>
/// <remarks>
/// Make the shim before the messenger starts.
/// </remarks>
public class HoldShim
{
    // The message. The payload is not used.
    const string ToggleSetupWindowMessage = "dev.eoinmotherway.hold.toggle-setup-window";

    // The events
    const string SetupWindowOpenedEvent = "dev.eoinmotherway.hold.setup-window-opened";
    const string SetupWindowClosedEvent = "dev.eoinmotherway.hold.setup-window-closed";

    // The menu item and the window of the Hold plugin. The menu item is in the vatSys Tools menu.
    const string ToolsItem = "toolsToolStripMenuItem";
    const string SetupItemText = "Hold Setup";
    const string SetupWindowTitle = "Hold Setup";

    readonly VatSysMenu _vatSysMenu;
    readonly ToggleableWindow _setupWindow;

    public HoldShim(
        VatSysMenu vatSysMenu,
        MenuEvents events,
        IPluginMessenger messenger,
        IMessenger vatSysMessenger,
        IGuiInvoker guiInvoker)
    {
        _vatSysMenu = vatSysMenu;

        _setupWindow = new ToggleableWindow(
            window => window.Text == SetupWindowTitle,
            ClickSetupItem,
            vatSysMessenger);

        // The Hold plugin will send the events to all plugins. A plugin does not receive its own messages, thus the shim
        // raises the events in this plugin.
        _setupWindow.IsOpenChanged += (_, _) =>
            events.Raise(_setupWindow.IsOpen ? SetupWindowOpenedEvent : SetupWindowClosedEvent);

        // The messenger receives the messages on a background thread
        messenger.Subscribe(ToggleSetupWindowMessage, (_, _) =>
        {
            guiInvoker.InvokeOnGUI(_ => _setupWindow.Toggle());
            return Task.CompletedTask;
        });
    }

    void ClickSetupItem()
    {
        var item = _vatSysMenu.FindPluginItem(ToolsItem, SetupItemText)
            ?? throw new InvalidOperationException($"Cannot find the menu item {SetupItemText}. The Hold plugin is not loaded.");

        item.PerformClick();
    }
}
