using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin.Shims;

/// <summary>
/// Handles the messages of the ATIS plugin, until the ATIS plugin does this itself. The shim opens the ATIS Editor
/// with the ATIS plugin menu item.
/// </summary>
/// <remarks>
/// Make the shim before the messenger starts.
/// </remarks>
public class AtisShim
{
    // The message. The payload is not used.
    const string OpenEditorMessage = "com.badvectors.atisplugin.open-atis-editor";

    // The menu item of the ATIS plugin. The menu item is in the vatSys Windows menu.
    const string WindowsItem = "windowsToolStripMenuItem";
    const string EditorItemText = "ATIS Editor";

    readonly VatSysMenu _vatSysMenu;

    public AtisShim(VatSysMenu vatSysMenu, IPluginMessenger messenger, IGuiInvoker guiInvoker)
    {
        _vatSysMenu = vatSysMenu;

        // The messenger receives the messages on a background thread
        messenger.Subscribe(OpenEditorMessage, (_, _) =>
        {
            guiInvoker.InvokeOnGUI(_ => ClickEditorItem());
            return Task.CompletedTask;
        });
    }

    void ClickEditorItem()
    {
        var item = _vatSysMenu.FindPluginItem(WindowsItem, EditorItemText)
            ?? throw new InvalidOperationException(
                $"Cannot find the menu item {EditorItemText}. The ATIS plugin is not loaded.");

        item.PerformClick();
    }
}
