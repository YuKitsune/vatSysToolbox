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
        menu.Resources.Add(typeof(TextBox), target.FindResource(typeof(TextBox)));

        foreach (var item in items)
            menu.Items.Add(CreateItem(menu, item, staysOpenOnClick));

        menu.IsOpen = true;
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
