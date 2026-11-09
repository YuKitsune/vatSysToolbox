using System.Windows.Forms;
using vatSysToolbox;

namespace EurocatMenusPlugin;

/// <summary>
/// Calls an action when the user presses the ALT key two times in the given interval.
/// The filter receives the key messages of all the windows on the UI thread, thus the user can press ALT in all
/// vatSys windows. The filter does not stop the messages.
/// </summary>
public class DoubleAltPressFilter(IClock clock, TimeSpan interval, Action onDoubleAltPress) : IMessageFilter
{
    const int WM_KEYDOWN = 0x0100;
    const int WM_SYSKEYDOWN = 0x0104;
    const int VK_MENU = 0x12;

    // Bit 30 of lParam is set when the key was already down. Windows sends these messages when the user holds a key.
    const long PreviousKeyStateFlag = 1L << 30;

    DateTimeOffset? _lastAltPress;

    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg != WM_KEYDOWN && m.Msg != WM_SYSKEYDOWN)
            return false;

        if ((m.LParam.ToInt64() & PreviousKeyStateFlag) != 0)
            return false;

        // A different key between the two ALT presses is not a double ALT press (for example, ALT+TAB)
        if (m.WParam.ToInt64() != VK_MENU)
        {
            _lastAltPress = null;
            return false;
        }

        var now = clock.UtcNow();
        if (_lastAltPress.HasValue && now - _lastAltPress.Value <= interval)
        {
            _lastAltPress = null;
            onDoubleAltPress();
        }
        else
        {
            _lastAltPress = now;
        }

        return false;
    }
}
