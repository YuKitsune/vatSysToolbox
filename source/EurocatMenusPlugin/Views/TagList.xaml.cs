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

        // The text has the same length as the text of a real strip, so that the sample strip has the same size
        SampleStrip.Content = new TagListItemViewModel(string.Empty, " A0000 000 ", string.Empty);
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
    /// Gets the size that shows one strip. This size does not change when strips are added or removed.
    /// </summary>
    public Size MeasureOneStrip()
    {
        var contentSize = MeasureContent();

        // The border and the scroll bar around the strips
        var frameWidth = contentSize.Width - StripList.DesiredSize.Width;
        var frameHeight = contentSize.Height - StripList.DesiredSize.Height;

        return new Size(
            frameWidth + SampleStrip.DesiredSize.Width,
            frameHeight + SampleStrip.DesiredSize.Height);
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
