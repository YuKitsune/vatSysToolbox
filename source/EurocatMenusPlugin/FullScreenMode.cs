using System.Windows.Forms;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin;

/// <summary>
/// Shows the vatSys main form on the full screen, with no title bar and no border. The main form then also covers the
/// taskbar. vatSys does not have a full screen mode.
/// </summary>
/// <remarks>
/// Make this class before the messenger starts, because it subscribes to its message.
/// </remarks>
public class FullScreenMode
{
    const string ToggleMessage = "dev.eoinmotherway.eurocatmenus.toggle-full-screen";
    const string OnEvent = "dev.eoinmotherway.eurocatmenus.full-screen-enabled";
    const string OffEvent = "dev.eoinmotherway.eurocatmenus.full-screen-disabled";

    readonly Form _mainForm;
    readonly MenuEvents _events;

    // The state of the main form before full screen, so that it can go back to this state
    FormBorderStyle _previousBorderStyle;
    FormWindowState _previousWindowState;

    bool _isOn;

    public FullScreenMode(Form mainForm, MenuEvents events, IPluginMessenger messenger, IGuiInvoker guiInvoker)
    {
        _mainForm = mainForm;
        _events = events;

        // The messenger receives the messages on a background thread
        messenger.Subscribe(ToggleMessage, (_, _) =>
        {
            guiInvoker.InvokeOnGUI(_ => Toggle());
            return Task.CompletedTask;
        });
    }

    void Toggle()
    {
        if (_isOn)
            TurnOff();
        else
            TurnOn();
    }

    void TurnOn()
    {
        _previousBorderStyle = _mainForm.FormBorderStyle;
        _previousWindowState = _mainForm.WindowState;

        // Windows calculates the size of a maximised form from its border. Thus, remove the border while the form is
        // not maximised, and then maximise it again.
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.FormBorderStyle = FormBorderStyle.None;
        _mainForm.WindowState = FormWindowState.Maximized;

        _isOn = true;
        _events.Publish(OnEvent);
    }

    void TurnOff()
    {
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.FormBorderStyle = _previousBorderStyle;
        _mainForm.WindowState = _previousWindowState;

        _isOn = false;
        _events.Publish(OffEvent);
    }
}
