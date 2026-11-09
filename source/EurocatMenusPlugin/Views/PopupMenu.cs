using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace EurocatMenusPlugin.Views;

/// <summary>
/// Opens a popup menu next to a button. The toolbox styles make it look like a vatSys menu.
/// </summary>
static class PopupMenu
{
    /// <param name="target">The button that opens the menu.</param>
    /// <param name="items">The items in the menu.</param>
    /// <param name="placement">The side of the button where the menu opens.</param>
    /// <param name="staysOpenOnClick">
    /// <c>true</c> to keep the menu open after a click, so that more than one item can be turned on or off
    /// (for example, maps).
    /// </param>
    public static void Open(
        FrameworkElement target,
        IEnumerable<PopupMenuItem> items,
        PlacementMode placement,
        bool staysOpenOnClick = false)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = target,
            Placement = placement
        };

        // The menu is not in the logical tree of the view. Thus, it cannot find the toolbox styles in the resources of
        // the view. Copy the styles from the view to the menu.
        menu.Style = (Style)target.FindResource(typeof(ContextMenu));
        menu.Resources.Add(typeof(MenuItem), target.FindResource(typeof(MenuItem)));
        menu.Resources.Add(MenuItem.SeparatorStyleKey, target.FindResource(MenuItem.SeparatorStyleKey));

        foreach (var item in items)
            menu.Items.Add(CreateItem(item, staysOpenOnClick));

        menu.IsOpen = true;
    }

    static Control CreateItem(PopupMenuItem item, bool staysOpenOnClick)
    {
        if (item.IsSeparator)
            return new Separator();

        var menuItem = new MenuItem
        {
            Header = item.Header,
            IsEnabled = item.IsEnabled,
            IsChecked = item.IsChecked,
            StaysOpenOnClick = staysOpenOnClick
        };

        foreach (var child in item.Children)
            menuItem.Items.Add(CreateItem(child, staysOpenOnClick));

        if (item.Click is { } click)
        {
            menuItem.Click += (_, e) =>
            {
                // The Click event goes up to the parent items. Stop it here, so that the parent items do nothing.
                e.Handled = true;
                click();
                menuItem.IsChecked = item.IsChecked;
            };
        }

        return menuItem;
    }
}
