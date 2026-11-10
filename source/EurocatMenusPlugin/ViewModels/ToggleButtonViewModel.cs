using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// A button that does an action, and looks pressed while it is on. Events set <see cref="IsChecked"/> (for example,
/// when a window opens or closes).
/// </summary>
public partial class ToggleButtonViewModel(string text, Action click) : ObservableObject, IBottomMenuItem
{
    [ObservableProperty] bool isChecked;

    public string Text { get; } = text;

    [RelayCommand]
    void Click()
    {
        click();

        // A toggle button changes its own state when it is clicked. Only the events change the state. Thus, send the
        // state again.
        OnPropertyChanged(nameof(IsChecked));
    }
}
