using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EurocatMenusPlugin.Views;

public partial class TextEntry : UserControl
{
    readonly Action<string> _submit;
    readonly Action _close;

    /// <param name="title">The text above the text box, or <c>null</c> for no title.</param>
    /// <param name="maxLength">The maximum number of characters.</param>
    /// <param name="isUpperCase"><c>true</c> to change the text to upper case.</param>
    /// <param name="submit">Gets the text when Enter is pressed.</param>
    /// <param name="close">Closes the popup. Enter and Escape close the popup.</param>
    public TextEntry(string? title, int maxLength, bool isUpperCase, Action<string> submit, Action close)
    {
        InitializeComponent();

        TitleText.Text = title;
        TitleText.Visibility = title is null ? Visibility.Collapsed : Visibility.Visible;

        InputBox.MaxLength = maxLength;
        InputBox.CharacterCasing = isUpperCase ? CharacterCasing.Upper : CharacterCasing.Normal;

        _submit = submit;
        _close = close;
    }

    void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                _close();
                _submit(InputBox.Text);
                break;

            case Key.Escape:
                e.Handled = true;
                _close();
                break;
        }
    }
}
