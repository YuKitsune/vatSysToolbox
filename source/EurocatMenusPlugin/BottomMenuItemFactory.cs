using System.Windows.Forms;
using EurocatMenusPlugin.Configuration;
using EurocatMenusPlugin.ViewModels;
using vatsys;
using vatSysToolbox;
using vatSysToolbox.Messaging;

namespace EurocatMenusPlugin;

/// <summary>
/// Makes the items of the bottom menu from the configuration. The built-in popup menus use the vatSys menu bar.
/// </summary>
/// <remarks>
/// The toggle buttons subscribe to their events. Thus, make the items before the messenger starts. Make the items on
/// the UI thread.
/// </remarks>
public class BottomMenuItemFactory(
    VatSysMenu vatSysMenu,
    VatSysInternals vatSysInternals,
    MenuEvents events,
    IPluginMessenger messenger,
    IErrorReporter errorReporter)
{
    // The names of the vatSys menu items
    const string MapsItem = "mapsToolStripMenuItem";
    const string InfoItem = "aISToolStripMenuItem";
    const string FindItem = "findToolStripMenuItem";
    const string WindowsItem = "windowsToolStripMenuItem";
    const string AtisItem = "aTISToolStripMenuItem";
    const string AtisRequestItem = "requestToolStripMenuItem";
    const string MessagesItem = "messagesToolStripMenuItem";
    const string SendMessageItem = "sendToolStripMenuItem";

    // In the vatSys ATIS menu, the online ATIS stations come after this separator
    const string AtisStationsSeparator = "toolStripSeparator4";

    // The menu items of other plugins
    // TODO: Use the messaging system to open the windows of other plugins, and not their menu items.
    const string AtisEditorText = "ATIS Editor";

    /// <summary>
    /// Makes the items. If an item is not correct, reports an error and does not make that item.
    /// </summary>
    public IReadOnlyList<IBottomMenuItem> CreateItems(IEnumerable<BottomMenuItemConfiguration> configurations)
    {
        var items = new List<IBottomMenuItem>();
        foreach (var configuration in configurations)
        {
            try
            {
                items.Add(CreateItem(configuration));
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, "Cannot make an item of the bottom menu");
            }
        }

        return items;
    }

    IBottomMenuItem CreateItem(BottomMenuItemConfiguration configuration)
    {
        return configuration switch
        {
            MenuItemConfiguration menu => CreateMenuItem(menu),
            IndicatorItemConfiguration indicator => CreateIndicator(indicator),
            StackItemConfiguration stack => new StackViewModel(CreateItems(stack.Items)),
            ToggleItemConfiguration toggle => CreateToggleItem(toggle),
            ButtonItemConfiguration button => CreateButtonItem(button),
            SeparatorItemConfiguration => throw new InvalidOperationException("A separator can only be in a popup menu"),
            _ => throw new InvalidOperationException($"The bottom menu item type {configuration.Type} is not known")
        };
    }

    IBottomMenuItem CreateMenuItem(MenuItemConfiguration configuration)
    {
        return configuration.Items switch
        {
            BuiltInMenuItemsConfiguration builtIn => CreateBuiltInMenuItem(
                configuration.Text,
                builtIn,
                configuration.StaysOpenOnClick),
            CustomMenuItemsConfiguration custom => new PopupButtonViewModel(
                configuration.Text,
                CreateGetPopupMenuItems(configuration.Text, custom),
                configuration.StaysOpenOnClick),
            _ => throw new InvalidOperationException($"The items of the menu {configuration.Text} are not known")
        };
    }

    PopupButtonViewModel CreateBuiltInMenuItem(
        string text,
        BuiltInMenuItemsConfiguration configuration,
        bool staysOpenOnClick)
    {
        return configuration.Name switch
        {
            "messages-menu" => CreateMessagesMenuItem(text, staysOpenOnClick),
            _ => new PopupButtonViewModel(text, CreateGetBuiltInMenuItems(configuration), staysOpenOnClick)
        };
    }

    // Gives a function that makes the items of the built-in menu
    Func<IReadOnlyList<PopupMenuItem>> CreateGetBuiltInMenuItems(BuiltInMenuItemsConfiguration configuration)
    {
        switch (configuration.Name)
        {
            case "map-menu":
            {
                var maps = vatSysMenu.GetPopupMenuItems(vatSysMenu.GetItem(MapsItem));
                return () => maps;
            }

            case "info-menu":
            {
                var infoItems = GetInfoMenuItems();
                return () => infoItems;
            }

            // The online ATIS stations, the private message recipients, and the unread messages change. Thus, get these
            // items each time that the menu opens.
            case "atis-menu":
                return () => GetAtisMenuItems(includeEditor: true);
            case "atis-menu-no-editor":
                return () => GetAtisMenuItems(includeEditor: false);
            case "messages-menu":
                return GetMessagesMenuItems;

            default:
                throw new InvalidOperationException(
                    $"The menu {BuiltInMenuItemsConfiguration.Prefix}{configuration.Name} is not known");
        }
    }

    IReadOnlyList<PopupMenuItem> GetInfoMenuItems()
    {
        // In vatSys, Find has a submenu with a text field. Here, Find opens the text box at once.
        var findItem = vatSysMenu.GetTextInputItem(vatSysMenu.GetItem(FindItem), "Find...", title: null);

        return vatSysMenu.GetPopupMenuItems(
            vatSysMenu.GetItem(InfoItem),
            new Dictionary<string, PopupMenuItem> { [FindItem] = findItem });
    }

    // Edit opens the ATIS Editor. View has Request, which opens an ATIS window for an airport, and the online ATIS
    // stations, which are the same as in the vatSys ATIS menu. Without the editor, the menu has only the items of View.
    IReadOnlyList<PopupMenuItem> GetAtisMenuItems(bool includeEditor)
    {
        try
        {
            var atisMenu = vatSysMenu.GetItem(AtisItem);

            // Request puts the ICAO code in the vatSys Request text field. Thus, vatSys opens the ATIS window, the
            // same as from its own menu.
            var requestItem = vatSysMenu.GetTextInputItem(
                vatSysMenu.GetItem(AtisRequestItem),
                "Request...",
                "Request ATIS");

            var stationItems = atisMenu.DropDownItems
                .Cast<ToolStripItem>()
                .SkipWhile(item => item.Name != AtisStationsSeparator)
                .Skip(1)
                .OfType<ToolStripMenuItem>()
                .Where(item => item.Available)
                .Select(item => vatSysMenu.GetPopupMenuItem(item));

            if (!includeEditor)
                return [requestItem, ..stationItems];

            var atisEditorItem = vatSysMenu.FindPluginItem(WindowsItem, AtisEditorText);
            return
            [
                new PopupMenuItem(
                    "Edit",
                    () => Run(() => ClickPluginItem(atisEditorItem)),
                    isEnabled: atisEditorItem is not null),
                new PopupMenuItem("View", children: [requestItem, ..stationItems])
            ];
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex);
            return [];
        }
    }

    static void ClickPluginItem(ToolStripMenuItem? item)
    {
        if (item is null)
            throw new InvalidOperationException("The plugin is not loaded");

        item.PerformClick();
    }

    // The button flashes while there are new messages
    PopupButtonViewModel CreateMessagesMenuItem(string text, bool staysOpenOnClick)
    {
        var messagesItem = vatSysMenu.GetItem(MessagesItem);
        var button = new PopupButtonViewModel(text, GetMessagesMenuItems, staysOpenOnClick)
        {
            IsFlashing = messagesItem.Checked
        };

        // vatSys checks its Messages item when a message is received in the System chat, the Controller chat, or a
        // private message. Then the vatSys menu bar flashes the item. Listen to the item, so that the button flashes at
        // once.
        messagesItem.CheckedChanged += (_, _) => button.IsFlashing = messagesItem.Checked;
        return button;
    }

    IReadOnlyList<PopupMenuItem> GetMessagesMenuItems()
    {
        try
        {
            var messagesItem = vatSysMenu.GetItem(MessagesItem);

            // Marks the new messages as seen, so that the button stops flashing. The same as a click on Messages in the
            // vatSys menu bar. The popup menu still shows the number of unread messages of each item.
            messagesItem.Checked = false;

            // In vatSys, Send has a submenu with a text field for the callsign. Here, Send opens the text box at once.
            var replacements = new Dictionary<string, PopupMenuItem>
            {
                [SendMessageItem] = vatSysMenu.GetTextInputItem(
                    vatSysMenu.GetItem(SendMessageItem),
                    "Send...",
                    title: null)
            };

            // vatSys opens a private message window when its item is released, and not when it is clicked. Thus, these
            // items open the window with the public vatSys method.
            foreach (var recipient in vatSysInternals.GetPrivateMessageRecipients())
            {
                if (messagesItem.DropDownItems[recipient] is not ToolStripMenuItem privateMessageItem)
                    continue;

                replacements[recipient] = new PopupMenuItem(
                    privateMessageItem.Text,
                    () => Run(() => OpenPrivateMessage(privateMessageItem)),
                    unreadCount: privateMessageItem.Tag is int count ? count : 0);
            }

            return vatSysMenu.GetPopupMenuItems(messagesItem, replacements);
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex);
            return [];
        }
    }

    // Shows the private message window, and marks its messages as read, the same as its item in the vatSys Messages
    // menu
    static void OpenPrivateMessage(ToolStripMenuItem privateMessageItem)
    {
        MMI.OpenPMWindow(privateMessageItem.Name);
        privateMessageItem.CheckState = CheckState.Unchecked;
        privateMessageItem.Tag = null;
    }

    // An exception that is not caught on the UI thread stops vatSys
    void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex);
        }
    }

    // Gives a function that makes the items of the menu. Make the items again each time that the popup menu opens, so
    // that they show the current state.
    Func<IReadOnlyList<PopupMenuItem>> CreateGetPopupMenuItems(string text, CustomMenuItemsConfiguration configuration)
    {
        var createItems = new List<Func<PopupMenuItem>>();
        foreach (var itemConfiguration in configuration.Items)
        {
            try
            {
                createItems.Add(CreatePopupMenuItem(itemConfiguration));
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, $"Cannot make an item of the {text} menu");
            }
        }

        return () => createItems.Select(createItem => createItem()).ToList();
    }

    // Checks the configuration, and gives a function that makes the item
    Func<PopupMenuItem> CreatePopupMenuItem(BottomMenuItemConfiguration configuration)
    {
        switch (configuration)
        {
            case SeparatorItemConfiguration:
                return () => PopupMenuItem.Separator;

            case MenuItemConfiguration menuConfiguration:
            {
                var getItems = CreateGetSubmenuItems(menuConfiguration);
                return () => new PopupMenuItem(menuConfiguration.Text, children: getItems());
            }

            case ToggleItemConfiguration toggleConfiguration:
            {
                var messageButton = new MessageButton(toggleConfiguration, messenger, errorReporter);
                var isChecked = CreateCheckedState(toggleConfiguration);
                return () => new PopupMenuItem(toggleConfiguration.Text, messageButton.Click, isChecked);
            }

            case ButtonItemConfiguration buttonConfiguration:
            {
                var messageButton = new MessageButton(buttonConfiguration, messenger, errorReporter);
                return () => new PopupMenuItem(buttonConfiguration.Text, messageButton.Click);
            }

            default:
                throw new InvalidOperationException($"A {configuration.Type} item cannot be in a popup menu");
        }
    }

    Func<IReadOnlyList<PopupMenuItem>> CreateGetSubmenuItems(MenuItemConfiguration configuration)
    {
        return configuration.Items switch
        {
            // In a submenu, a built-in menu does not flash, and it uses the staysOpenOnClick value of its top-level
            // menu. It only gives its items.
            BuiltInMenuItemsConfiguration builtIn => CreateGetBuiltInMenuItems(builtIn),
            CustomMenuItemsConfiguration custom => CreateGetPopupMenuItems(configuration.Text, custom),
            _ => throw new InvalidOperationException($"The items of the menu {configuration.Text} are not known")
        };
    }

    // The item shows a check mark after the on event, until the off event
    Func<bool> CreateCheckedState(ToggleItemConfiguration configuration)
    {
        CheckEvents(configuration);

        var isChecked = false;
        events.Subscribe(configuration.ActiveOn, () => isChecked = true);
        events.Subscribe(configuration.InactiveOn, () => isChecked = false);
        return () => isChecked;
    }

    IBottomMenuItem CreateIndicator(IndicatorItemConfiguration configuration)
    {
        var indicator = new IndicatorViewModel(configuration.Default);
        if (configuration.Event is null)
            return indicator;

        if (configuration.Template is null)
            throw new InvalidOperationException($"The indicator for the event {configuration.Event} must have a template");

        // Read the template now, so that an error in the template shows when the plugin starts
        var template = new TextTemplate(configuration.Template);
        events.Subscribe(configuration.Event, payload =>
        {
            // If the template cannot use the payload, keep the last value
            try
            {
                indicator.Text = template.Render(payload);
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, $"Cannot show the event {configuration.Event} in the indicator");
            }
        });

        return indicator;
    }

    IBottomMenuItem CreateButtonItem(ButtonItemConfiguration configuration)
    {
        var messageButton = new MessageButton(configuration, messenger, errorReporter);
        return new ButtonViewModel(configuration.Text, messageButton.Click);
    }

    // The events turn the button on and off
    IBottomMenuItem CreateToggleItem(ToggleItemConfiguration configuration)
    {
        CheckEvents(configuration);

        var messageButton = new MessageButton(configuration, messenger, errorReporter);
        var button = new ToggleButtonViewModel(configuration.Text, messageButton.Click);
        events.Subscribe(configuration.ActiveOn, () => button.IsChecked = true);
        events.Subscribe(configuration.InactiveOn, () => button.IsChecked = false);
        return button;
    }

    static void CheckEvents(ToggleItemConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.ActiveOn) || string.IsNullOrEmpty(configuration.InactiveOn))
            throw new InvalidOperationException(
                $"The toggle {configuration.Text} must have activeOn and inactiveOn");
    }
}
