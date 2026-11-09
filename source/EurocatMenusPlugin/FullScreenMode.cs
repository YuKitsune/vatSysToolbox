using System.Windows.Forms;

namespace EurocatMenusPlugin;

/// <summary>
/// Shows the vatSys main form on the full screen, with no title bar and no border. The main form then also covers the
/// taskbar. vatSys does not have a full screen mode.
/// </summary>
public class FullScreenMode(Form mainForm)
{
    // The state of the main form before full screen, so that it can go back to this state
    FormBorderStyle _previousBorderStyle;
    FormWindowState _previousWindowState;

    public bool IsOn { get; private set; }

    public void Toggle()
    {
        if (IsOn)
            TurnOff();
        else
            TurnOn();
    }

    void TurnOn()
    {
        _previousBorderStyle = mainForm.FormBorderStyle;
        _previousWindowState = mainForm.WindowState;

        // Windows calculates the size of a maximised form from its border. Thus, remove the border while the form is
        // not maximised, and then maximise it again.
        mainForm.WindowState = FormWindowState.Normal;
        mainForm.FormBorderStyle = FormBorderStyle.None;
        mainForm.WindowState = FormWindowState.Maximized;

        IsOn = true;
    }

    void TurnOff()
    {
        mainForm.WindowState = FormWindowState.Normal;
        mainForm.FormBorderStyle = _previousBorderStyle;
        mainForm.WindowState = _previousWindowState;

        IsOn = false;
    }
}
