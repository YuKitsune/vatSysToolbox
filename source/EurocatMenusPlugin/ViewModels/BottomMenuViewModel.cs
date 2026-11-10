namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// The items of the bottom menu. <see cref="BottomMenuItemFactory"/> makes the items from the configuration.
/// </summary>
public class BottomMenuViewModel
{
    // Example data for the XAML designer. Do not use at runtime.
    public BottomMenuViewModel()
        : this(
        [
            new PopupButtonViewModel("Set Up", () => []),
            new ButtonViewModel("FPL", () => { }),
            new StackViewModel(
            [
                new ToggleButtonViewModel("SSTP", () => { }) { IsChecked = true },
                new IndicatorViewModel("A")
            ])
        ])
    {
    }

    /// <param name="items">The items of the bottom menu, from the configuration.</param>
    public BottomMenuViewModel(IReadOnlyList<IBottomMenuItem> items)
    {
        Items = items;
    }

    public IReadOnlyList<IBottomMenuItem> Items { get; }
}
