using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EurocatMenusPlugin.ViewModels;

namespace EurocatMenusPlugin.Views;

public partial class TagList : UserControl
{
    readonly TagListViewModel _viewModel;

    public TagList(TagListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
    }

    /// <summary>
    /// Gets the size that shows all the strips, with no space on the right of the strips.
    /// </summary>
    public Size MeasureContent()
    {
        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return DesiredSize;
    }

    /// <summary>
    /// Gets the size that shows only the first strip.
    /// </summary>
    public Size MeasureOneStrip()
    {
        var contentSize = MeasureContent();

        // The measure makes the strips. Thus, the first strip has its size now.
        if (StripList.ItemContainerGenerator.ContainerFromIndex(0) is not UIElement firstStrip)
            return contentSize;

        // The border and the scroll bar around the strips, and the first strip
        var otherStripsHeight = StripList.DesiredSize.Height - firstStrip.DesiredSize.Height;
        return new Size(contentSize.Width, contentSize.Height - otherStripsHeight);
    }

    void Acid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        if (sender is not FrameworkElement { DataContext: TagListItemViewModel item })
            return;

        _viewModel.RemoveQuickTagCommand.Execute(item);
        e.Handled = true;
    }

    void GlobalOps_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: TagListItemViewModel item })
            return;

        item.EditGlobalOpsCommand.Execute(null);
        e.Handled = true;
    }

    void GlobalOpsTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: TagListItemViewModel item })
            return;

        item.CommitGlobalOpsEditCommand.Execute(null);
    }

    void GlobalOpsTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox textBox || e.NewValue is not true)
            return;

        textBox.Focus();
        textBox.SelectAll();
    }

    void GlobalOpsTextBox_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not TextBox { DataContext: TagListItemViewModel item })
            return;

        item.CancelGlobalOpsEditCommand.Execute(null);
    }
}
