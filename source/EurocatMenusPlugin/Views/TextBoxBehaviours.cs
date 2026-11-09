using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EurocatMenusPlugin.Views;

/// <summary>
/// Behaviours for the text boxes in the menus.
/// </summary>
public static class TextBoxBehaviours
{
    /// <summary>
    /// When <c>true</c>, the text box sends its text to the binding source only when Enter is pressed.
    /// If the text box loses the keyboard focus, or Escape is pressed, the text box shows the value from the binding
    /// source again. Set <c>UpdateSourceTrigger=Explicit</c> on the <c>Text</c> binding.
    /// </summary>
    public static readonly DependencyProperty CommitOnEnterProperty = DependencyProperty.RegisterAttached(
        "CommitOnEnter",
        typeof(bool),
        typeof(TextBoxBehaviours),
        new PropertyMetadata(false, OnCommitOnEnterChanged));

    /// <summary>
    /// A regular expression for one character. The text box rejects the characters that do not match.
    /// </summary>
    public static readonly DependencyProperty AllowedCharactersProperty = DependencyProperty.RegisterAttached(
        "AllowedCharacters",
        typeof(string),
        typeof(TextBoxBehaviours),
        new PropertyMetadata(null, OnAllowedCharactersChanged));

    public static bool GetCommitOnEnter(TextBox textBox) => (bool)textBox.GetValue(CommitOnEnterProperty);

    public static void SetCommitOnEnter(TextBox textBox, bool value) => textBox.SetValue(CommitOnEnterProperty, value);

    public static string? GetAllowedCharacters(TextBox textBox) => (string?)textBox.GetValue(AllowedCharactersProperty);

    public static void SetAllowedCharacters(TextBox textBox, string? value) => textBox.SetValue(AllowedCharactersProperty, value);

    static void OnCommitOnEnterChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not TextBox textBox)
            return;

        textBox.KeyDown -= CommitOrRevert;
        textBox.LostKeyboardFocus -= Revert;

        if (!(bool)args.NewValue)
            return;

        textBox.KeyDown += CommitOrRevert;
        textBox.LostKeyboardFocus += Revert;
    }

    static void CommitOrRevert(object sender, KeyEventArgs e)
    {
        var binding = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
        if (binding is null)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                binding.UpdateSource();

                // The binding source can reject the value. Then the text box shows the value from the source.
                binding.UpdateTarget();
                e.Handled = true;
                break;

            case Key.Escape:
                binding.UpdateTarget();
                e.Handled = true;
                break;
        }
    }

    static void Revert(object sender, KeyboardFocusChangedEventArgs e)
    {
        ((TextBox)sender).GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
    }

    static void OnAllowedCharactersChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not TextBox textBox)
            return;

        textBox.PreviewTextInput -= RejectCharacters;
        DataObject.RemovePastingHandler(textBox, RejectPaste);

        if (args.NewValue is null)
            return;

        textBox.PreviewTextInput += RejectCharacters;
        DataObject.AddPastingHandler(textBox, RejectPaste);
    }

    static void RejectCharacters(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsAllowed((TextBox)sender, e.Text);
    }

    static void RejectPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(typeof(string)) is not string text || !IsAllowed((TextBox)sender, text))
            e.CancelCommand();
    }

    static bool IsAllowed(TextBox textBox, string text)
    {
        var pattern = GetAllowedCharacters(textBox);
        return pattern is null || text.All(character => Regex.IsMatch(character.ToString(), $"^{pattern}$"));
    }
}
