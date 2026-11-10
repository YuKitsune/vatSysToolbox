using CommunityToolkit.Mvvm.ComponentModel;

namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// A button that opens a popup menu.
/// </summary>
/// <param name="getItems">Gets the items of the popup menu each time that it opens.</param>
/// <param name="staysOpenOnClick">If <c>true</c>, the popup menu stays open after a click on an item.</param>
public partial class PopupButtonViewModel(
    string text,
    Func<IReadOnlyList<PopupMenuItem>> getItems,
    bool staysOpenOnClick = false,
    Func<bool>? getIsEnabled = null)
    : ObservableObject, IBottomMenuItem
{
    [ObservableProperty] bool isEnabled = true;

    /// <summary>
    /// If <c>true</c>, the background of the button changes between the warning colour and the window colour.
    /// </summary>
    [ObservableProperty] bool isFlashing;

    public string Text { get; } = text;

    /// <summary>
    /// If <c>true</c>, the popup menu stays open after a click on an item.
    /// </summary>
    public bool StaysOpenOnClick { get; } = staysOpenOnClick;

    /// <summary>
    /// Gets the items of the popup menu. The button calls this each time that the popup menu opens.
    /// </summary>
    public IReadOnlyList<PopupMenuItem> GetItems() => getItems();

    public void Refresh()
    {
        IsEnabled = getIsEnabled?.Invoke() ?? true;
    }
}
