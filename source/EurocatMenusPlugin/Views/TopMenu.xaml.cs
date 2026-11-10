using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using EurocatMenusPlugin.ViewModels;

namespace EurocatMenusPlugin.Views;

public partial class TopMenu : UserControl
{
    readonly TopMenuViewModel _viewModel;

    public TopMenu(TopMenuViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
    }

    // The menu is at the top of the screen. Thus, the popup menus open below the buttons.

    void PositionButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetPositionMenuItems(), PlacementMode.Bottom);
    }

    void VisibilityCentersButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetVisibilityCentersMenuItems(), PlacementMode.Bottom);
    }

    void VelocityVectorLengthButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetVelocityVectorLengthMenuItems(), PlacementMode.Bottom);
    }

    void ShortRouteProbeLengthButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetShortRouteProbeLengthMenuItems(), PlacementMode.Bottom);
    }

    void HistoryDotCountButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetHistoryDotCountMenuItems(), PlacementMode.Bottom);
    }
}
