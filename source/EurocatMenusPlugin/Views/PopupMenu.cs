using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using vatSysToolbox.Controls;

namespace EurocatMenusPlugin.Views;

/// <summary>
/// Opens a popup menu next to a button. Each item in the menu is a button.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>The menu is at least as wide as the button that opens it. It can be wider.</item>
/// <item>The button that opens the menu is a toggle button. It is on while the menu is open, thus it looks pressed.</item>
/// <item>
/// An item with a submenu shows a chevron. A click on the item opens its submenu, and the item looks pressed while the
/// submenu is open. Each level can have only one open submenu.
/// </item>
/// <item>
/// A submenu stays open until its item is clicked again, another submenu on the same level opens, or its parent menu
/// closes.
/// </item>
/// <item>
/// If a menu or a submenu is higher than the screen, it shows its items in pages. A Next button at the bottom shows the
/// next page, and a Previous button at the top shows the previous page.
/// </item>
/// </list>
/// This menu does not use the WPF context menu. A context menu opens and closes its submenus when the mouse moves.
/// </remarks>
static class PopupMenu
{
    static readonly Uri ResourcesUri = new("/EurocatMenusPlugin;component/Views/PopupMenuResources.xaml", UriKind.Relative);

    /// <param name="target">The button that opens the menu.</param>
    /// <param name="items">The items in the menu.</param>
    /// <param name="placement">The side of the button where the menu opens.</param>
    /// <param name="staysOpenOnClick">
    /// <c>true</c> to keep the menu open after a click, so that more than one item can be turned on or off
    /// (for example, maps).
    /// </param>
    public static void Open(
        ToggleButton target,
        IEnumerable<PopupMenuItem> items,
        PlacementMode placement,
        bool staysOpenOnClick = false)
    {
        var menu = CreatePopup(target, placement);
        menu.StaysOpen = false;

        // The popup is not in the logical tree of the view. Thus, give it its own styles. The submenus are in the
        // logical tree of the popup, thus they also find these styles.
        menu.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = ResourcesUri });

        var context = new MenuContext(menu, target, placement, staysOpenOnClick);
        var panel = CreatePanel(context, menu, items, GetAvailableHeight(target, placement));
        panel.MinWidth = target.ActualWidth;

        BindTarget(target, menu);
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

    // vatSys menus open and close at once. A popup has no animation by default.
    static Popup CreatePopup(UIElement placementTarget, PlacementMode placement)
    {
        return new Popup
        {
            PlacementTarget = placementTarget,
            Placement = placement,
            AllowsTransparency = true
        };
    }

    // The height in which the popup can open, in device-independent units. A popup on the top or the bottom of the
    // button must not cover the button. A submenu can move up or down to fit, thus it can use the height of the screen.
    // WPF keeps a popup in the bounds of the screen, not in the working area.
    static double GetAvailableHeight(ToggleButton target, PlacementMode placement)
    {
        var top = target.PointToScreen(new Point(0, 0)).Y;
        var bottom = target.PointToScreen(new Point(0, target.ActualHeight)).Y;
        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(0, (int)top)).Bounds;

        var pixels = placement switch
        {
            PlacementMode.Top => top - screen.Top,
            PlacementMode.Bottom => screen.Bottom - bottom,
            _ => screen.Height
        };

        var pixelsToUnits = PresentationSource.FromVisual(target)?.CompositionTarget?.TransformFromDevice.M22 ?? 1;
        return pixels * pixelsToUnits;
    }

    /// <summary>
    /// Makes the panel of a menu or a submenu, and shows it in the popup.
    /// </summary>
    /// <param name="context">The menu that contains this panel.</param>
    /// <param name="owner">The popup that shows this panel.</param>
    /// <param name="items">The items in the panel.</param>
    /// <param name="availableHeight">The height in which the panel can open. If the panel is higher, it uses pages.</param>
    static FrameworkElement CreatePanel(
        MenuContext context,
        Popup owner,
        IEnumerable<PopupMenuItem> items,
        double availableHeight)
    {
        var stack = new StackPanel();

        // The buttons of the items and the separators, without the submenu popups
        var itemButtons = new List<FrameworkElement>();

        // The submenu that is open on this level
        Popup? openSubmenu = null;

        // When this menu closes, its open submenu closes too
        owner.Closed += (_, _) =>
        {
            if (openSubmenu is not null)
                openSubmenu.IsOpen = false;
        };

        foreach (var item in items)
        {
            // A separator is a blank button that does nothing
            if (item.IsSeparator)
            {
                var separator = new ToggleButton { IsEnabled = false };
                separator.SetResourceReference(FrameworkElement.StyleProperty, "PopupMenuItemStyle");
                stack.Children.Add(separator);
                itemButtons.Add(separator);
                continue;
            }

            var button = new ToggleButton
            {
                Content = CreateContent(item),
                IsEnabled = item.IsEnabled
            };
            button.SetResourceReference(FrameworkElement.StyleProperty, "PopupMenuItemStyle");
            stack.Children.Add(button);
            itemButtons.Add(button);

            if (item.Children.Count > 0)
            {
                var submenu = CreatePopup(button, PlacementMode.Right);
                submenu.StaysOpen = true;
                submenu.Closed += (_, _) => button.IsChecked = false;

                // The submenu must be in the logical tree of this menu, so that it finds the styles, and so that a
                // click in the submenu is a click inside this menu. Add it before its panel is made, because the
                // panel finds the styles when it measures its items.
                stack.Children.Add(submenu);
                CreatePanel(context, submenu, item.Children, GetAvailableHeight(context.Target, PlacementMode.Right));

                button.Click += (_, _) =>
                {
                    if (submenu.IsOpen)
                    {
                        submenu.IsOpen = false;
                        return;
                    }

                    if (openSubmenu is not null)
                        openSubmenu.IsOpen = false;

                    openSubmenu = submenu;
                    submenu.IsOpen = true;
                    button.IsChecked = true;
                };
            }
            else if (item.Submit is { } submit)
            {
                button.Click += (_, _) =>
                {
                    button.IsChecked = false;
                    OpenTextEntryAfterClose(context, item, submit);
                };
            }
            else
            {
                button.IsChecked = item.IsChecked;
                button.Click += (_, _) =>
                {
                    item.Click?.Invoke();

                    // A toggle button changes its own state when it is clicked. Show the state from vatSys.
                    button.IsChecked = item.IsChecked;

                    if (!context.StaysOpenOnClick)
                        context.Menu.IsOpen = false;
                };
            }
        }

        var panel = new Border { Child = stack };
        panel.SetResourceReference(FrameworkElement.StyleProperty, "PopupMenuPanelStyle");
        owner.Child = panel;

        // The submenu of an item on another page must not stay open
        Paginate(panel, stack, itemButtons, availableHeight, () =>
        {
            if (openSubmenu is not null)
                openSubmenu.IsOpen = false;
        });

        return panel;
    }

    // If the panel is higher than the available height, shows the items in pages. Each page has a Previous button at
    // the top (except the first page), and a Next button at the bottom (except the last page).
    static void Paginate(
        Border panel,
        StackPanel stack,
        IReadOnlyList<FrameworkElement> itemButtons,
        double availableHeight,
        Action pageChanged)
    {
        // The panel must be in the popup before it is measured, so that it finds its styles
        panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        if (panel.DesiredSize.Height <= availableHeight || itemButtons.Count == 0)
            return;

        // The background can have a border or a padding around the items
        var pageHeight = availableHeight - (panel.DesiredSize.Height - stack.DesiredSize.Height);
        var pages = CreatePages(itemButtons, pageHeight);
        if (pages.Count < 2)
            return;

        // The widest item sets the width of all the pages, so that the width does not change on each page
        stack.MinWidth = stack.DesiredSize.Width;

        var previous = CreatePageButton("Previous");
        var next = CreatePageButton("Next");
        stack.Children.Insert(0, previous);
        stack.Children.Add(next);

        var pageIndex = 0;
        previous.Click += (_, _) => ShowPage(pageIndex - 1);
        next.Click += (_, _) => ShowPage(pageIndex + 1);
        ShowPage(0);

        void ShowPage(int index)
        {
            pageChanged();
            pageIndex = index;

            foreach (var itemButton in itemButtons)
                itemButton.Visibility = Visibility.Collapsed;

            foreach (var itemButton in pages[index])
                itemButton.Visibility = Visibility.Visible;

            previous.Visibility = index > 0 ? Visibility.Visible : Visibility.Collapsed;
            next.Visibility = index < pages.Count - 1 ? Visibility.Visible : Visibility.Collapsed;

            // A toggle button changes its own state when it is clicked
            previous.IsChecked = false;
            next.IsChecked = false;
        }
    }

    // Puts as many items as possible on each page. Each page keeps space for the Previous and the Next buttons.
    static List<List<FrameworkElement>> CreatePages(IReadOnlyList<FrameworkElement> itemButtons, double pageHeight)
    {
        // The Previous and the Next buttons have the same style as the items
        var pageButtonHeight = itemButtons.Max(itemButton => itemButton.DesiredSize.Height);

        var pages = new List<List<FrameworkElement>>();
        var page = new List<FrameworkElement>();

        // The first page has no Previous button
        var height = 0.0;

        foreach (var itemButton in itemButtons)
        {
            if (page.Count > 0 && height + itemButton.DesiredSize.Height + pageButtonHeight > pageHeight)
            {
                pages.Add(page);
                page = [];
                height = pageButtonHeight;
            }

            page.Add(itemButton);
            height += itemButton.DesiredSize.Height;
        }

        pages.Add(page);
        return pages;
    }

    static ToggleButton CreatePageButton(string text)
    {
        var content = new DockPanel();
        content.SetResourceReference(FrameworkElement.StyleProperty, "PopupMenuItemContentStyle");
        content.Children.Add(new TextBlock { Text = text });

        var button = new ToggleButton { Content = content };
        button.SetResourceReference(FrameworkElement.StyleProperty, "PopupMenuItemStyle");
        return button;
    }

    // The header on the left, and the unread count and the chevron on the right
    static DockPanel CreateContent(PopupMenuItem item)
    {
        var content = new DockPanel();
        content.SetResourceReference(FrameworkElement.StyleProperty, "PopupMenuItemContentStyle");

        if (item.Children.Count > 0)
        {
            var chevron = new Chevron();
            chevron.SetResourceReference(FrameworkElement.StyleProperty, "PopupMenuChevronStyle");
            DockPanel.SetDock(chevron, Dock.Right);
            content.Children.Add(chevron);
        }

        if (item.UnreadCount > 0)
        {
            var unreadCount = new TextBlock { Text = $"[{item.UnreadCount}]" };
            unreadCount.SetResourceReference(FrameworkElement.StyleProperty, "PopupMenuUnreadCountStyle");
            DockPanel.SetDock(unreadCount, Dock.Right);
            content.Children.Add(unreadCount);
        }

        content.Children.Add(new TextBlock { Text = item.Header });
        return content;
    }

    // The text box is in a separate popup at the button that opened the menu. The menu closes first. When the menu
    // closes, it sets the button to closed. Thus, open the text entry popup only after the menu is closed.
    static void OpenTextEntryAfterClose(MenuContext context, PopupMenuItem item, Action<string> submit)
    {
        context.Menu.Closed += OpenAfterClose;
        context.Menu.IsOpen = false;

        void OpenAfterClose(object sender, EventArgs e)
        {
            context.Menu.Closed -= OpenAfterClose;
            OpenTextEntry(context.Target, context.Placement, item, submit);
        }
    }

    static void OpenTextEntry(ToggleButton target, PlacementMode placement, PopupMenuItem item, Action<string> submit)
    {
        // The same popup settings as the custom period popup in the CPDLC plugin. Typing works in that popup.
        var popup = CreatePopup(target, placement);
        popup.StaysOpen = false;
        popup.Child = new TextEntry(item.Title, item.MaxLength, item.IsUpperCase, submit, () => popup.IsOpen = false);

        BindTarget(target, popup);
        popup.IsOpen = true;
    }

    sealed class MenuContext(Popup menu, ToggleButton target, PlacementMode placement, bool staysOpenOnClick)
    {
        /// <summary>
        /// The popup of the top level of the menu.
        /// </summary>
        public Popup Menu { get; } = menu;

        /// <summary>
        /// The button that opened the menu.
        /// </summary>
        public ToggleButton Target { get; } = target;

        /// <summary>
        /// The side of the button where the menu opened.
        /// </summary>
        public PlacementMode Placement { get; } = placement;

        /// <summary>
        /// <c>true</c> to keep the menu open after a click on an item.
        /// </summary>
        public bool StaysOpenOnClick { get; } = staysOpenOnClick;
    }
}
