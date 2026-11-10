using CommunityToolkit.Mvvm.ComponentModel;

namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// A small box that shows a value (for example, the strip sort mode). Events set <see cref="Text"/>.
/// </summary>
public partial class IndicatorViewModel(string defaultText) : ObservableObject, IBottomMenuItem
{
    [ObservableProperty] string text = defaultText;
}
