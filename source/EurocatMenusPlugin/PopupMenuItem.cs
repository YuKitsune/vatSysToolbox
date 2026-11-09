namespace EurocatMenusPlugin;

/// <summary>
/// An item in a popup menu. An item with children opens a nested popup menu.
/// </summary>
public class PopupMenuItem(
    string header,
    Action? click = null,
    Func<bool>? isChecked = null,
    bool isEnabled = true,
    IReadOnlyList<PopupMenuItem>? children = null)
{
    public static PopupMenuItem Separator { get; } = new(string.Empty) { IsSeparator = true };

    public string Header { get; } = header;

    public Action? Click { get; } = click;

    public bool IsEnabled { get; } = isEnabled;

    public IReadOnlyList<PopupMenuItem> Children { get; } = children ?? [];

    public bool IsSeparator { get; private set; }

    /// <summary>
    /// The popup menu reads this value when it opens, and again after a click.
    /// </summary>
    public bool IsChecked => isChecked?.Invoke() ?? false;
}
