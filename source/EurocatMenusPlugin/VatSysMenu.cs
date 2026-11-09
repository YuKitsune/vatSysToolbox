using System.Windows.Forms;
using vatSysToolbox;

namespace EurocatMenusPlugin;

/// <summary>
/// The vatSys menu bar. The vatSys menu bar is hidden, but its items still work.
/// The buttons click these items, so that vatSys does the work and keeps its menu bar correct.
/// </summary>
/// <remarks>
/// vatSys does not make the menu items public. Thus, this class finds them by their names. A vatSys update can change
/// these names. Then this class throws an exception.
/// </remarks>
public class VatSysMenu(MenuStrip menuStrip, IErrorReporter errorReporter)
{
    // A plugin menu item that has a custom category is in a top-level menu item with this name
    const string PluginCategorySuffix = "MenuItem";

    /// <summary>
    /// Gets a vatSys menu item, for example <c>flightPlanToolStripMenuItem</c>.
    /// </summary>
    public ToolStripMenuItem GetItem(string name)
    {
        return menuStrip.Items.Find(name, searchAllChildren: true).OfType<ToolStripMenuItem>().FirstOrDefault()
            ?? throw new InvalidOperationException($"Cannot find the vatSys menu item {name}");
    }

    /// <summary>
    /// Gets a menu item that a plugin added to a vatSys menu item. Gives <c>null</c> if the plugin is not loaded.
    /// </summary>
    /// <param name="parentName">The name of the vatSys menu item that contains the plugin menu item.</param>
    /// <param name="text">The text of the plugin menu item.</param>
    public ToolStripMenuItem? FindPluginItem(string parentName, string text)
    {
        var parent = menuStrip.Items.Find(parentName, searchAllChildren: true).OfType<ToolStripMenuItem>().FirstOrDefault();
        return parent?.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(item => item.Text == text);
    }

    /// <summary>
    /// Gets the top-level menu item that vatSys makes for a custom plugin category (for example, <c>CPDLC</c>).
    /// Gives <c>null</c> if the plugin is not loaded.
    /// </summary>
    public ToolStripMenuItem? FindPluginCategory(string category)
    {
        return menuStrip.Items[category + PluginCategorySuffix] as ToolStripMenuItem;
    }

    public void Click(string name)
    {
        GetItem(name).PerformClick();
    }

    /// <summary>
    /// Makes popup menu items from the children of a vatSys menu item.
    /// </summary>
    public IReadOnlyList<PopupMenuItem> GetPopupMenuItems(ToolStripMenuItem parent)
    {
        var items = new List<PopupMenuItem>();
        foreach (ToolStripItem item in parent.DropDownItems)
        {
            if (!item.Available)
                continue;

            switch (item)
            {
                case ToolStripSeparator:
                    items.Add(PopupMenuItem.Separator);
                    break;
                case ToolStripMenuItem menuItem:
                    items.Add(GetPopupMenuItem(menuItem));
                    break;

                // vatSys puts text fields in some menus (for example, Info, Find). The popup menu shows an item with
                // the name of the menu (for example, Find...) that opens a text box.
                case ToolStripControlHost { Control: TextBox textField }:
                    items.Add(PopupMenuItem.TextInput(
                        parent.Text + "...",
                        text => Submit(textField, text),
                        textField.MaxLength,
                        textField.CharacterCasing == CharacterCasing.Upper));
                    break;
            }
        }

        return items;
    }

    /// <summary>
    /// Makes a popup menu item from a vatSys menu item. A click on the popup menu item clicks the vatSys menu item.
    /// </summary>
    public PopupMenuItem GetPopupMenuItem(ToolStripMenuItem item, string? header = null)
    {
        var children = GetPopupMenuItems(item);
        return new PopupMenuItem(
            header ?? item.Text,
            children.Count == 0 ? () => PerformClick(item) : null,
            () => item.Checked,
            item.Enabled,
            children);
    }

    // Puts the text in the vatSys text field, and presses Enter in it. The vatSys text field does its action when it
    // gets Enter as a key press. A key press can only be sent with its protected method, thus use reflection.
    void Submit(TextBox textField, string text)
    {
        try
        {
            textField.Text = text;
            Reflection.InvokeMethod(textField, "OnKeyPress", new KeyPressEventArgs('\r'));
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, "Cannot send the text to vatSys");
        }
    }

    // An exception that is not caught on the UI thread stops vatSys.
    void PerformClick(ToolStripMenuItem item)
    {
        try
        {
            item.PerformClick();
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, $"Cannot do {item.Text}");
        }
    }
}
