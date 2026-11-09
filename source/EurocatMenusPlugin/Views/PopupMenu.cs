using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace EurocatMenusPlugin.Views;

/// <summary>
/// Opens a popup menu next to a button. The toolbox styles make it look like a vatSys menu.
/// </summary>
/// <remarks>
/// The button is a toggle button. It is on while its popup is open, thus it looks pressed.
/// </remarks>
static class PopupMenu
{
    /// <param name="target">The button that opens the menu.</param>
    /// <param name="items">The items in the menu.</param>
    /// <param name="placement">The side of the button where the menu opens.</param>
    /// <param name="staysOpenOnClick">
    /// <c>true</c> to keep the menu open after a click, so that more than one item can be turned on or off
    /// (for example, maps).
    /// </param>
    /// <param name="matchTargetWidth"><c>true</c> to make the menu the same width as the button.</param>
    public static void Open(
        ToggleButton target,
        IEnumerable<PopupMenuItem> items,
        PlacementMode placement,
        bool staysOpenOnClick = false,
        bool matchTargetWidth = false)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = target,
            Placement = placement
        };

        if (matchTargetWidth)
            menu.Width = target.ActualWidth;

        // The menu is not in the logical tree of the view. Thus, it cannot find the toolbox styles in the resources of
        // the view. Copy the styles from the view to the menu.
        menu.Style = (Style)target.FindResource(typeof(ContextMenu));
        menu.Resources.Add(typeof(MenuItem), target.FindResource(typeof(MenuItem)));
        menu.Resources.Add(MenuItem.SeparatorStyleKey, target.FindResource(MenuItem.SeparatorStyleKey));

        foreach (var item in items)
            menu.Items.Add(CreateItem(menu, target, placement, item, staysOpenOnClick));

        RemoveAnimation(menu);
        SetTargetOpen(target);
        menu.Closed += (_, _) => SetTargetClosed(target);
        menu.IsOpen = true;
    }

    /// <summary>
    /// Makes the button look pressed while the popup is open, and makes a click on the button close the popup.
    /// Call this each time before the popup opens.
    /// </summary>
    public static void BindTarget(ToggleButton target, Popup popup)
    {
        SetTargetOpen(target);
        popup.Closed += Restore;

        void Restore(object sender, EventArgs e)
        {
            popup.Closed -= Restore;
            SetTargetClosed(target);
        }
    }

    // While the popup is open, the button does not get the mouse. Thus, a click on the button is a click outside the
    // popup. Then the popup closes, and the button does not open it again.
    static void SetTargetOpen(ToggleButton target)
    {
        target.IsChecked = true;
        target.IsHitTestVisible = false;
    }

    static void SetTargetClosed(ToggleButton target)
    {
        target.IsChecked = false;
        target.IsHitTestVisible = true;
    }

    // vatSys menus open and close at once. A WPF context menu fades in and out if Windows animates its menus.
    // The context menu makes its popup when it opens, and gives the popup the Windows menu animation. Thus, make the
    // popup now, and remove the animation before the menu opens. The method that makes the popup is private.
    static void RemoveAnimation(ContextMenu menu)
    {
        try
        {
            Reflection.InvokeMethod(menu, "HookupParentPopup");
        }
        catch (MissingMemberException)
        {
            // An exception that is not caught on the UI thread stops vatSys. Without the method, the menu still works,
            // but it fades.
            return;
        }

        if (menu.Parent is Popup popup)
            popup.PopupAnimation = PopupAnimation.None;
    }

    static Control CreateItem(
        ContextMenu menu,
        ToggleButton target,
        PlacementMode placement,
        PopupMenuItem item,
        bool staysOpenOnClick)
    {
        if (item.IsSeparator)
            return new Separator();

        if (item.Submit is { } submit)
            return CreateTextInputItem(menu, target, placement, item, submit);

        var menuItem = new MenuItem
        {
            Header = item.Header,
            IsEnabled = item.IsEnabled,
            IsChecked = item.IsChecked,
            StaysOpenOnClick = staysOpenOnClick
        };

        foreach (var child in item.Children)
            menuItem.Items.Add(CreateItem(menu, target, placement, child, staysOpenOnClick));

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

    // A text box in a context menu does not get keyboard input, because the menu takes the keyboard. Thus, the item
    // opens a plain popup with the text box at the same button. The context menu closes when the item is clicked.
    static MenuItem CreateTextInputItem(
        ContextMenu menu,
        ToggleButton target,
        PlacementMode placement,
        PopupMenuItem item,
        Action<string> submit)
    {
        var menuItem = new MenuItem { Header = item.Header };
        menuItem.Click += (_, e) =>
        {
            // The Click event goes up to the parent items. Stop it here, so that the parent items do nothing.
            e.Handled = true;

            // When the context menu closes, it sets the button to closed. Thus, open the text entry popup only after
            // the context menu is closed.
            if (!menu.IsOpen)
            {
                OpenTextEntry(target, placement, item, submit);
                return;
            }

            menu.Closed += OpenAfterClose;

            void OpenAfterClose(object sender, RoutedEventArgs args)
            {
                menu.Closed -= OpenAfterClose;
                OpenTextEntry(target, placement, item, submit);
            }
        };

        return menuItem;
    }

    static void OpenTextEntry(ToggleButton target, PlacementMode placement, PopupMenuItem item, Action<string> submit)
    {
        // The same popup settings as the custom period popup in the CPDLC plugin. Typing works in that popup.
        var popup = new Popup
        {
            PlacementTarget = target,
            Placement = placement,
            StaysOpen = false,
            AllowsTransparency = true
        };

        popup.Child = new TextEntry(item.MaxLength, item.IsUpperCase, submit, () => popup.IsOpen = false);

        BindTarget(target, popup);
        popup.IsOpen = true;
    }
}
