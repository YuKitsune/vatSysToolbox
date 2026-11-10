using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using EurocatMenusPlugin.ViewModels;

namespace EurocatMenusPlugin.Views;

public partial class BottomMenu : UserControl
{
    public BottomMenu(BottomMenuViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    // The menu is at the bottom of the screen. Thus, the popup menus open above the buttons.
    void PopupButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { DataContext: PopupButtonViewModel popupButton } button)
            return;

        PopupMenu.Open(
            button,
            popupButton.GetItems(),
            PlacementMode.Top,
            popupButton.StaysOpenOnClick);
    }
}
