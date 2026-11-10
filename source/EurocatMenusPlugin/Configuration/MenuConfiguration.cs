namespace EurocatMenusPlugin.Configuration;

/// <summary>
/// The contents of the configuration file.
/// </summary>
public class MenuConfiguration
{
    /// <summary>
    /// The items of the bottom menu, from left to right.
    /// </summary>
    public List<BottomMenuItemConfiguration> BottomMenu { get; set; } = [];
}

/// <summary>
/// An item of the bottom menu. The <see cref="Type"/> key in the file sets the kind of item.
/// </summary>
public abstract class BottomMenuItemConfiguration
{
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// <c>type: menu</c>. A button that opens a popup menu. In a popup menu, an item that opens a submenu.
/// </summary>
public class MenuItemConfiguration : BottomMenuItemConfiguration
{
    public string Text { get; set; } = string.Empty;

    public MenuItemsConfiguration Items { get; set; } = new CustomMenuItemsConfiguration([]);

    /// <summary>
    /// If <c>true</c>, the popup menu stays open after a click on an item, so that more than one item can be turned on
    /// or off (for example, maps). Not used for a submenu. A submenu uses the value of its top-level menu.
    /// </summary>
    public bool StaysOpenOnClick { get; set; }
}

/// <summary>
/// The items of a popup menu. In the file, this is a built-in menu (for example, <c>builtin/map-menu</c>), or a list
/// of items.
/// </summary>
public abstract class MenuItemsConfiguration;

/// <summary>
/// The items of a built-in menu.
/// </summary>
/// <param name="name">The name of the built-in menu, with no prefix (for example, <c>map-menu</c>).</param>
public class BuiltInMenuItemsConfiguration(string name) : MenuItemsConfiguration
{
    public const string Prefix = "builtin/";

    public string Name { get; } = name;
}

/// <summary>
/// The items of a custom menu. Only <c>type: button</c>, <c>type: toggle</c>, <c>type: menu</c>, and
/// <c>type: separator</c> items can be in a popup menu.
/// </summary>
public class CustomMenuItemsConfiguration(List<BottomMenuItemConfiguration> items) : MenuItemsConfiguration
{
    public List<BottomMenuItemConfiguration> Items { get; } = items;
}

/// <summary>
/// <c>type: indicator</c>. A small box that shows a value. The value changes when an event occurs.
/// </summary>
public class IndicatorItemConfiguration : BottomMenuItemConfiguration
{
    /// <summary>
    /// The value before the first event.
    /// </summary>
    public string Default { get; set; } = string.Empty;

    /// <summary>
    /// The event that changes the value. If not set, the indicator always shows <see cref="Default"/>.
    /// </summary>
    public string? Event { get; set; }

    /// <summary>
    /// Makes the value from the payload of the event, in the Scriban syntax (for example,
    /// <c>{{ mode | string.slice 0 1 }}</c>). The properties of the payload are variables. Required if
    /// <see cref="Event"/> is set.
    /// </summary>
    public string? Template { get; set; }
}

/// <summary>
/// <c>type: separator</c>. A blank item between groups of items in a popup menu.
/// </summary>
public class SeparatorItemConfiguration : BottomMenuItemConfiguration;

/// <summary>
/// <c>type: stack</c>. Items on top of each other, in the space of one button.
/// </summary>
public class StackItemConfiguration : BottomMenuItemConfiguration
{
    public List<BottomMenuItemConfiguration> Items { get; set; } = [];
}

/// <summary>
/// <c>type: button</c>. A button that sends a message to other plugins.
/// </summary>
public class ButtonItemConfiguration : BottomMenuItemConfiguration
{
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The name of the plugin that receives the message. If not set, all the other plugins receive the message.
    /// </summary>
    public string? Recipient { get; set; }

    /// <summary>
    /// The type of the message (for example, <c>dev.eoinmotherway.tag-list.toggle</c>).
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// The data of the message. The button sends it as JSON. If not set, the button sends an empty JSON object.
    /// </summary>
    public object? Payload { get; set; }
}

/// <summary>
/// <c>type: toggle</c>. A button that sends a message to other plugins, and looks pressed after
/// <see cref="ActiveOn"/>, until <see cref="InactiveOn"/>. In a popup menu, the item shows a check mark.
/// </summary>
/// <remarks>
/// An event is a built-in event (for example, <c>dev.eoinmotherway.eurocatmenus.strip-setup-window-opened</c>), or a
/// message from another plugin. The name of the event is the type of the message.
/// </remarks>
public class ToggleItemConfiguration : ButtonItemConfiguration
{
    /// <summary>
    /// The event that makes the toggle active.
    /// </summary>
    public string ActiveOn { get; set; } = string.Empty;

    /// <summary>
    /// The event that makes the toggle inactive.
    /// </summary>
    public string InactiveOn { get; set; } = string.Empty;
}
