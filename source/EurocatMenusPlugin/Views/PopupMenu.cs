using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

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
    /// <param name="matchTargetWidth"><c>true</c> to make the menu the same width as the button.</param>
    public static void Open(
        FrameworkElement target,
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
        menu.Resources.Add(typeof(TextBox), target.FindResource(typeof(TextBox)));

        foreach (var item in items)
            menu.Items.Add(CreateItem(menu, item, staysOpenOnClick));

        RemoveAnimation(menu);
        CloseOnTargetClick(target, menu);
        menu.IsOpen = true;
    }

    /// <summary>
    /// Makes a click on the button close the popup, and not open it again.
    /// </summary>
    /// <remarks>
    /// While the popup is open, the button does not get the mouse. Thus, a click on the button is a click outside
    /// the popup, and the popup closes.
    /// </remarks>
    static void CloseOnTargetClick(FrameworkElement target, ContextMenu menu)
    {
        target.IsHitTestVisible = false;
        menu.Closed += (_, _) => target.IsHitTestVisible = true;
    }

    /// <summary>
    /// Makes a click on the button close the popup, and not open it again. Call this each time before the popup
    /// opens.
    /// </summary>
    public static void CloseOnTargetClick(FrameworkElement target, Popup popup)
    {
        target.IsHitTestVisible = false;
        popup.Closed += Restore;

        void Restore(object sender, EventArgs e)
        {
            popup.Closed -= Restore;
            target.IsHitTestVisible = true;
        }
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

    static Control CreateItem(ContextMenu menu, PopupMenuItem item, bool staysOpenOnClick)
    {
        if (item.IsSeparator)
            return new Separator();

        if (item.Submit is { } submit)
            return CreateTextInputItem(menu, item, submit);

        var menuItem = new MenuItem
        {
            Header = item.Header,
            IsEnabled = item.IsEnabled,
            IsChecked = item.IsChecked,
            StaysOpenOnClick = staysOpenOnClick
        };

        foreach (var child in item.Children)
            menuItem.Items.Add(CreateItem(menu, child, staysOpenOnClick));

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

    static MenuItem CreateTextInputItem(ContextMenu menu, PopupMenuItem item, Action<string> submit)
    {
        var textBox = new TextBox
        {
            MaxLength = item.MaxLength,
            CharacterCasing = item.IsUpperCase ? CharacterCasing.Upper : CharacterCasing.Normal,
            MinWidth = 80
        };

        textBox.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            menu.IsOpen = false;
            submit(textBox.Text);
        };

        // Put the cursor in the text box when its menu opens, the same as vatSys
        textBox.IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue)
                textBox.Dispatcher.BeginInvoke(new Action(() => textBox.Focus()));
        };

        return new MenuItem
        {
            Header = textBox,

            // A menu item takes the keyboard focus when the mouse moves on it. Then the text box loses the focus.
            Focusable = false,

            // A click in the text box must not close the menu
            StaysOpenOnClick = true
        };
    }
}
