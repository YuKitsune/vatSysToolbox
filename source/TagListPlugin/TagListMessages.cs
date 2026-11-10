namespace TagListPlugin;

/// <summary>
/// The messages that other plugins (for example, the buttons of the Eurocat Menus plugin) use to control the Tag List.
/// Send these messages to the plugin with the name <see cref="Plugin.Name"/>. The plugin ignores the payload.
/// </summary>
public static class TagListMessages
{
    /// <summary>
    /// Shows or hides the Tag List.
    /// </summary>
    public const string Toggle = "dev.eoinmotherway.tag-list.toggle";

    /// <summary>
    /// Opens the vatSys Quick Tag window for the selected track.
    /// </summary>
    public const string AddQuickTag = "dev.eoinmotherway.tag-list.add-quick-tag";

    /// <summary>
    /// Removes the quick tag of the selected track.
    /// </summary>
    public const string RemoveQuickTag = "dev.eoinmotherway.tag-list.remove-quick-tag";

    /// <summary>
    /// The plugin sends this message to all plugins when the Tag List is shown. The payload is an empty JSON object.
    /// </summary>
    public const string Opened = "dev.eoinmotherway.tag-list.opened";

    /// <summary>
    /// The plugin sends this message to all plugins when the Tag List is hidden. The payload is an empty JSON object.
    /// </summary>
    public const string Closed = "dev.eoinmotherway.tag-list.closed";
}
