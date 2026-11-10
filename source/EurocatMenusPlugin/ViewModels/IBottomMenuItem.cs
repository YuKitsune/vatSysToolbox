namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// An item of the bottom menu. The bottom menu has a view for each kind of item.
/// </summary>
public interface IBottomMenuItem
{
    /// <summary>
    /// Reads the state of the item again.
    /// </summary>
    void Refresh();
}
