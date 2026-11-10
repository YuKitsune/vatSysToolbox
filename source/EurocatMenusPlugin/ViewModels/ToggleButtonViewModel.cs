using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// A button that does an action, and looks pressed while it is on. Events set <see cref="IsChecked"/> (for example,
/// when a window opens or closes).
/// </summary>
public partial class ToggleButtonViewModel(string text, Action click, Func<bool>? getIsEnabled = null)
    : ObservableObject, IBottomMenuItem
{
    [ObservableProperty] bool isChecked;
    [ObservableProperty] bool isEnabled = true;

    public string Text { get; } = text;

    public void Refresh()
    {
        IsEnabled = getIsEnabled?.Invoke() ?? true;
    }

    [RelayCommand]
    void Click()
    {
        click();

        // A toggle button changes its own state when it is clicked. Only the events change the state. Thus, send the
        // state again.
        OnPropertyChanged(nameof(IsChecked));
    }
}
