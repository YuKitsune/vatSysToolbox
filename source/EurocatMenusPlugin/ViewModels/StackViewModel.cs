namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// Items on top of each other, in the space of one button.
/// </summary>
public class StackViewModel(IReadOnlyList<IBottomMenuItem> items) : IBottomMenuItem
{
    public IReadOnlyList<IBottomMenuItem> Items { get; } = items;

    public void Refresh()
    {
        foreach (var item in Items)
            item.Refresh();
    }
}
