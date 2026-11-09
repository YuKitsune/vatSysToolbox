using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using EurocatMenusPlugin.ViewModels;

namespace EurocatMenusPlugin.Views;

public partial class BottomMenu : UserControl
{
    readonly BottomMenuViewModel _viewModel;

    public BottomMenu(BottomMenuViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
    }

    // The menu is at the bottom of the screen. Thus, the popup menus open above the buttons.

    void SetUpButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetSetUpMenuItems(), PlacementMode.Top);
    }

    void MapButton_Click(object sender, RoutedEventArgs e)
    {
        // vatSys keeps the Maps menu open after a click, so that more than one map can be turned on or off
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetMapMenuItems(), PlacementMode.Top, staysOpenOnClick: true);
    }

    void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetInfoMenuItems(), PlacementMode.Top);
    }

    void TfmsButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetTfmsMenuItems(), PlacementMode.Top, matchTargetWidth: true);
    }

    void AtisButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetAtisMenuItems(), PlacementMode.Top, matchTargetWidth: true);
    }

    void MessagesButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetMessagesMenuItems(), PlacementMode.Top);
    }
}
