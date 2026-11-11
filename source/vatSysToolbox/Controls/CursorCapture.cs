using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rectangle = System.Drawing.Rectangle;
using WinFormsCursor = System.Windows.Forms.Cursor;

namespace vatSysToolbox.Controls;

/// <summary>
/// Keeps the mouse cursor in a text box while the text box has the keyboard focus. When the text box gets the focus,
/// the cursor moves to the center of the text box. Enter, Escape, or a middle-click removes the focus from the text box,
/// and releases the cursor.
/// </summary>
/// <example>
/// <code>&lt;TextBox controls:CursorCapture.IsEnabled="True" /&gt;</code>
/// </example>
public static class CursorCapture
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(CursorCapture),
        new PropertyMetadata(false, OnIsEnabledChanged));

    // The cursor can be in only one area. This is the text box that has the cursor, or null.
    static TextBox? _capturingTextBox;

    public static bool GetIsEnabled(TextBox textBox) => (bool)textBox.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(TextBox textBox, bool value) => textBox.SetValue(IsEnabledProperty, value);

    static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not TextBox textBox)
            return;

        textBox.GotKeyboardFocus -= Capture;
        textBox.LostKeyboardFocus -= Release;
        textBox.Unloaded -= Release;
        textBox.PreviewMouseDown -= ReleaseOnMiddleClick;
        textBox.RemoveHandler(UIElement.KeyUpEvent, (KeyEventHandler)ReleaseOnEnterOrEscape);

        if (!(bool)args.NewValue)
        {
            Release(textBox);
            return;
        }

        textBox.GotKeyboardFocus += Capture;
        textBox.LostKeyboardFocus += Release;
        textBox.Unloaded += Release;
        textBox.PreviewMouseDown += ReleaseOnMiddleClick;

        // Remove the focus after the key up, and not on the key down. Then the other key down handlers of the text box
        // (for example, a handler that saves the text on Enter) run while the text box still has the focus.
        textBox.AddHandler(UIElement.KeyUpEvent, (KeyEventHandler)ReleaseOnEnterOrEscape, handledEventsToo: true);

        if (textBox.IsKeyboardFocused)
            Capture(textBox);
    }

    static void Capture(object sender, KeyboardFocusChangedEventArgs e) => Capture((TextBox)sender);

    static void Capture(TextBox textBox)
    {
        if (PresentationSource.FromVisual(textBox) is null)
            return;

        // PointToScreen gives physical pixels. The cursor also uses physical pixels.
        var topLeft = textBox.PointToScreen(new Point(0, 0));
        var bottomRight = textBox.PointToScreen(new Point(textBox.ActualWidth, textBox.ActualHeight));
        var bounds = Rectangle.FromLTRB(
            (int)Math.Round(topLeft.X),
            (int)Math.Round(topLeft.Y),
            (int)Math.Round(bottomRight.X),
            (int)Math.Round(bottomRight.Y));

        WinFormsCursor.Position = new System.Drawing.Point(
            bounds.Left + bounds.Width / 2,
            bounds.Top + bounds.Height / 2);
        WinFormsCursor.Clip = bounds;
        _capturingTextBox = textBox;
    }

    static void Release(object sender, RoutedEventArgs e) => Release((TextBox)sender);

    static void Release(TextBox textBox)
    {
        // Do not release the cursor from a different text box
        if (_capturingTextBox != textBox)
            return;

        WinFormsCursor.Clip = Rectangle.Empty;
        _capturingTextBox = null;
    }

    static void ReleaseOnEnterOrEscape(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
            RemoveFocus((TextBox)sender);
    }

    static void ReleaseOnMiddleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        e.Handled = true;
        RemoveFocus((TextBox)sender);
    }

    // Release the cursor first, also if the text box cannot lose the focus
    static void RemoveFocus(TextBox textBox)
    {
        Release(textBox);

        if (textBox.IsKeyboardFocused)
            Keyboard.ClearFocus();
    }
}
