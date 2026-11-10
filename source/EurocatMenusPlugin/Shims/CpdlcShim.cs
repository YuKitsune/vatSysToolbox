using System.Windows.Forms;
using CommunityToolkit.Mvvm.Messaging;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin.Shims;

/// <summary>
/// Handles the messages of the CPDLC plugin, and sends its events, until the CPDLC plugin does this itself. The shim
/// opens and closes the windows with the CPDLC menu items.
/// </summary>
/// <remarks>
/// Make the shim before the messenger starts.
/// </remarks>
public class CpdlcShim
{
    // The messages. The payload is not used.
    const string ToggleSetupWindowMessage = "dev.eoinmotherway.cpdlc.toggle-setup-window";
    const string ToggleAdsContractWindowMessage = "dev.eoinmotherway.cpdlc.toggle-ads-contract-window";
    const string ToggleHistoryWindowMessage = "dev.eoinmotherway.cpdlc.toggle-history-window";

    // The events
    const string SetupWindowOpenedEvent = "dev.eoinmotherway.cpdlc.setup-window-opened";
    const string SetupWindowClosedEvent = "dev.eoinmotherway.cpdlc.setup-window-closed";
    const string AdsContractWindowOpenedEvent = "dev.eoinmotherway.cpdlc.ads-contract-window-opened";
    const string AdsContractWindowClosedEvent = "dev.eoinmotherway.cpdlc.ads-contract-window-closed";
    const string HistoryWindowOpenedEvent = "dev.eoinmotherway.cpdlc.history-window-opened";
    const string HistoryWindowClosedEvent = "dev.eoinmotherway.cpdlc.history-window-closed";

    // The menu items and the windows of the CPDLC plugin
    const string CpdlcCategory = "CPDLC";
    const string SetupItemText = "Setup";
    const string AdsContractItemText = "ADS Contract";
    const string HistoryItemText = "History";
    const string SetupWindowTitle = "CPDLC Setup";
    const string AdsContractWindowTitle = "ADS Contract";
    const string HistoryWindowTitle = "CPDLC History";

    readonly VatSysMenu _vatSysMenu;
    readonly ToggleableWindow _setupWindow;
    readonly ToggleableWindow _adsContractWindow;
    readonly ToggleableWindow _historyWindow;

    public CpdlcShim(
        VatSysMenu vatSysMenu,
        MenuEvents events,
        IPluginMessenger messenger,
        IMessenger vatSysMessenger,
        IGuiInvoker guiInvoker)
    {
        _vatSysMenu = vatSysMenu;

        _setupWindow = new ToggleableWindow(
            window => window.Text == SetupWindowTitle,
            () => ClickCpdlcItem(SetupItemText),
            vatSysMessenger);
        _adsContractWindow = new ToggleableWindow(
            window => window.Text == AdsContractWindowTitle,
            () => ClickCpdlcItem(AdsContractItemText),
            vatSysMessenger);
        _historyWindow = new ToggleableWindow(
            window => window.Text == HistoryWindowTitle,
            () => ClickCpdlcItem(HistoryItemText),
            vatSysMessenger);

        // The CPDLC plugin will send the events to all plugins. A plugin does not receive its own messages, thus the shim
        // raises the events in this plugin.
        _setupWindow.IsOpenChanged += (_, _) =>
            events.Raise(_setupWindow.IsOpen ? SetupWindowOpenedEvent : SetupWindowClosedEvent);
        _adsContractWindow.IsOpenChanged += (_, _) =>
            events.Raise(_adsContractWindow.IsOpen ? AdsContractWindowOpenedEvent : AdsContractWindowClosedEvent);
        _historyWindow.IsOpenChanged += (_, _) =>
            events.Raise(_historyWindow.IsOpen ? HistoryWindowOpenedEvent : HistoryWindowClosedEvent);

        // The messenger receives the messages on a background thread
        messenger.Subscribe(ToggleSetupWindowMessage, (_, _) =>
        {
            guiInvoker.InvokeOnGUI(_ => _setupWindow.Toggle());
            return Task.CompletedTask;
        });
        messenger.Subscribe(ToggleAdsContractWindowMessage, (_, _) =>
        {
            guiInvoker.InvokeOnGUI(_ => _adsContractWindow.Toggle());
            return Task.CompletedTask;
        });
        messenger.Subscribe(ToggleHistoryWindowMessage, (_, _) =>
        {
            guiInvoker.InvokeOnGUI(_ => _historyWindow.Toggle());
            return Task.CompletedTask;
        });
    }

    void ClickCpdlcItem(string text)
    {
        var item = _vatSysMenu.FindPluginCategory(CpdlcCategory)?.DropDownItems
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(menuItem => menuItem.Text == text)
            ?? throw new InvalidOperationException($"Cannot find the CPDLC menu item {text}. The CPDLC plugin is not loaded.");

        item.PerformClick();
    }
}
