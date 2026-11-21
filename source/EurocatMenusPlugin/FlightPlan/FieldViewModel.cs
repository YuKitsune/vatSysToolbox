using CommunityToolkit.Mvvm.ComponentModel;

namespace EurocatMenusPlugin.FlightPlan;

/// <summary>
/// One data field of the Flight Plan Window. The view binds a text box to it.
/// </summary>
/// <remarks>
/// <see cref="Text"/> changes when the user types, and when the view model calls <see cref="Set"/>. Only the user
/// makes the field <see cref="IsModified"/>. <see cref="Set"/> clears it, the same as the <c>Modified</c> property of
/// a WinForms text box.
/// </remarks>
/// <param name="maxLength">The maximum number of characters, or 0 for no limit.</param>
/// <param name="allowedCharacters">
/// A regular expression for one character. The field rejects the characters that do not match. <c>null</c> allows all
/// characters.
/// </param>
public partial class FieldViewModel(int maxLength = 0, string? allowedCharacters = null) : ObservableObject
{
    bool _isSetting;

    /// <summary>
    /// The maximum number of characters, or 0 for no limit.
    /// </summary>
    public int MaxLength { get; } = maxLength;

    /// <summary>
    /// A regular expression for one character, or <c>null</c> to allow all characters.
    /// </summary>
    public string? AllowedCharacters { get; } = allowedCharacters;

    [ObservableProperty] string text = "";

    /// <summary>
    /// <c>true</c> if the user can type in the field. An editable field has a sunken bevel, and a display-only field
    /// has a thin outline.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReadOnly))]
    bool isEditable;

    /// <summary>
    /// <c>true</c> to show the field with the warning colour.
    /// </summary>
    [ObservableProperty] bool hasError;

    /// <summary>
    /// The opposite of <see cref="IsEditable"/>, for the <c>IsReadOnly</c> property of the text box.
    /// </summary>
    public bool IsReadOnly => !IsEditable;

    /// <summary>
    /// <c>true</c> once the user has changed the text since the view model last set it.
    /// </summary>
    public bool IsModified { get; set; }

    /// <summary>
    /// Occurs when the field asks for the keyboard focus.
    /// </summary>
    public event EventHandler? FocusRequested;

    /// <summary>
    /// Occurs when the field asks the text box to select all its text.
    /// </summary>
    public event EventHandler? SelectAllRequested;

    /// <summary>
    /// Sets the text from the view model. The field is then not <see cref="IsModified"/>.
    /// </summary>
    public void Set(string? value)
    {
        _isSetting = true;
        try
        {
            Text = value ?? "";
        }
        finally
        {
            _isSetting = false;
        }

        IsModified = false;
    }

    partial void OnTextChanged(string value)
    {
        if (!_isSetting)
            IsModified = true;
    }

    /// <summary>
    /// Asks for the keyboard focus. The view gives the focus only if the field is editable.
    /// </summary>
    public void Focus() => FocusRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Asks the text box to select all its text.
    /// </summary>
    public void SelectAll() => SelectAllRequested?.Invoke(this, EventArgs.Empty);
}
