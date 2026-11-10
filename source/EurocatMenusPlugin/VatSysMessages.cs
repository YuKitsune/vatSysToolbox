using System.Windows.Forms;

namespace EurocatMenusPlugin;

// The vatSys events that VatSysPatches sends with the weak reference messenger. vatSys can send them on any thread.

/// <summary>
/// vatSys updated its clock. vatSys does this each second.
/// </summary>
public class ClockUpdatedMessage;

/// <summary>
/// A display setting possibly changed (for example, the range, the filters, or the velocity vectors).
/// </summary>
public class DisplaySettingsChangedMessage;

/// <summary>
/// A window was shown, hidden, or activated.
/// </summary>
public class WindowChangedMessage(Form window)
{
    public Form Window { get; } = window;
}

/// <summary>
/// A window closed. The window can still be in the list of open windows.
/// </summary>
public class WindowClosedMessage(Form window)
{
    public Form Window { get; } = window;
}
