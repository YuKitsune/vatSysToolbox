using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// A button that does an action.
/// </summary>
public partial class ButtonViewModel(string text, Action click) : ObservableObject, IBottomMenuItem
{
    public string Text { get; } = text;

    public void Refresh()
    {
    }

    [RelayCommand]
    void Click() => click();
}
