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

    /// <summary>
    /// Makes an item that opens a text box. When Enter is pressed, the item sends the text to
    /// <paramref name="submit"/>.
    /// </summary>
    public static PopupMenuItem TextInput(string header, Action<string> submit, int maxLength, bool isUpperCase)
    {
        return new PopupMenuItem(header)
        {
            Submit = submit,
            MaxLength = maxLength,
            IsUpperCase = isUpperCase
        };
    }

    public string Header { get; } = header;

    public Action? Click { get; } = click;

    public bool IsEnabled { get; } = isEnabled;

    public IReadOnlyList<PopupMenuItem> Children { get; } = children ?? [];

    public bool IsSeparator { get; private set; }

    /// <summary>
    /// The action for the text of a text input item, or <c>null</c> if the item is not a text input item.
    /// </summary>
    public Action<string>? Submit { get; private set; }

    public int MaxLength { get; private set; }

    public bool IsUpperCase { get; private set; }

    /// <summary>
    /// The popup menu reads this value when it opens, and again after a click.
    /// </summary>
    public bool IsChecked => isChecked?.Invoke() ?? false;
}
