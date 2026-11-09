using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetVelocityVectorLengthMenuItems(), PlacementMode.Bottom, matchTargetWidth: true);
    }

    void ShortRouteProbeLengthButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetShortRouteProbeLengthMenuItems(), PlacementMode.Bottom, matchTargetWidth: true);
    }

    void HistoryDotCountButton_Click(object sender, RoutedEventArgs e)
    {
        PopupMenu.Open((ToggleButton)sender, _viewModel.GetHistoryDotCountMenuItems(), PlacementMode.Bottom, matchTargetWidth: true);
    }

    void AtisWindowButton_Click(object sender, RoutedEventArgs e)
    {
        AtisWindowList.Children.Clear();
        foreach (var item in _viewModel.GetAtisWindowMenuItems())
        {
            var menuItem = new MenuItem { Header = item.Header };
            menuItem.Click += (_, _) =>
            {
                AtisPopup.IsOpen = false;
                item.Click?.Invoke();
            };

            AtisWindowList.Children.Add(menuItem);
        }

        AtisWindowSeparator.Visibility = AtisWindowList.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        AtisAirportTextBox.Clear();
        PopupMenu.BindTarget((ToggleButton)sender, AtisPopup);
        AtisPopup.IsOpen = true;
    }

    void AtisAirportTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        AtisPopup.IsOpen = false;
        _viewModel.OpenAtisWindow(AtisAirportTextBox.Text);
    }
}
