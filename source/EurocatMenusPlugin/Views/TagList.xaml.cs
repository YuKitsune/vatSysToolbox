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
