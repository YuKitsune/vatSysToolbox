using System.Collections;
using System.Windows.Forms;
using vatsys;

namespace EurocatMenusPlugin;

/// <summary>
/// The vatSys state that the vatSys menu bar uses, but that vatSys does not make public.
/// </summary>
/// <remarks>
/// These members are private or internal in vatSys. Thus, this class uses reflection.
/// </remarks>
public class VatSysInternals(Form mainForm)
{
    const string AsdControlName = "ASD";
    const string AirWindowTypeName = "ASDWindow";

    // The Air windows that are open. The window that was activated last is at the end.
    readonly List<Form> _airWindows = [];

    /// <summary>
    /// The ASD in the vatSys main form.
    /// </summary>
    public AsdDisplay MainAsd { get; } = new(
        mainForm.Controls[AsdControlName] ?? throw new InvalidOperationException("Cannot find the ASD"));

    /// <summary>
    /// The velocity vector length that vatSys uses when velocity vectors are turned on again.
    /// </summary>
    public int StoredVelocityVectorLength => (int)Reflection.GetField(mainForm, "storedVelValue")!;

    /// <summary>
    /// The number of history dots that vatSys uses when history dots are turned on again.
    /// </summary>
    public int StoredHistoryDotCount => (int)Reflection.GetField(mainForm, "storedHistValue")!;

    /// <summary>
    /// The time when the timer goes off, or <see cref="DateTime.MaxValue"/> if the timer is not set.
    /// </summary>
    public DateTime TimerTime => (DateTime)Reflection.GetField(mainForm, "TimerTime")!;

    /// <summary>
    /// The ASD in the Air window that was selected last, or <c>null</c> if no Air window is open.
    /// </summary>
    /// <remarks>
    /// vatSys does not record which Air window was selected last. Thus, this class records the activation of each Air
    /// window from when it finds the window. Until an Air window is activated after that, this method uses the Air
    /// window that was opened last.
    /// </remarks>
    public AsdDisplay? FindSecondAsd()
    {
        WatchNewAirWindows();

        var airWindow = _airWindows.LastOrDefault();
        return airWindow is null
            ? null
            : new AsdDisplay((Control)Reflection.GetField(airWindow, AsdControlName)!);
    }

    // Application.OpenForms gives the windows in the order that they were opened. Thus, the window that was opened
    // last goes at the end.
    void WatchNewAirWindows()
    {
        var newAirWindows = Application.OpenForms
            .Cast<Form>()
            .Where(form => form.GetType().Name == AirWindowTypeName && !_airWindows.Contains(form))
            .ToList();

        foreach (var airWindow in newAirWindows)
        {
            _airWindows.Add(airWindow);
            airWindow.Activated += AirWindow_Activated;
            airWindow.FormClosed += AirWindow_FormClosed;
        }
    }

    void AirWindow_Activated(object sender, EventArgs e)
    {
        var airWindow = (Form)sender;
        _airWindows.Remove(airWindow);
        _airWindows.Add(airWindow);
    }

    void AirWindow_FormClosed(object sender, FormClosedEventArgs e)
    {
        var airWindow = (Form)sender;
        airWindow.Activated -= AirWindow_Activated;
        airWindow.FormClosed -= AirWindow_FormClosed;
        _airWindows.Remove(airWindow);
    }

    /// <summary>
    /// The callsigns of the private message windows. vatSys has an item in its Messages menu for each of them. The name
    /// of the item is the callsign.
    /// </summary>
    public IReadOnlyList<string> GetPrivateMessageRecipients()
    {
        var windows = (IEnumerable)Reflection.GetStaticProperty(typeof(MMI), "PrivateMessageWindows")!;
        return windows
            .Cast<object>()
            .Select(window => (string)Reflection.GetField(window, "Recipient")!)
            .ToList();
    }

    /// <summary>
    /// Opens the vatSys Time menu at the mouse position, the same as a click on the clock in the vatSys menu bar.
    /// </summary>
    public void OpenTimeMenu() => ClickClock(MouseButtons.Left);

    /// <summary>
    /// Acknowledges the timer and clears it, the same as a middle click on the clock in the vatSys menu bar.
    /// </summary>
    public void AcknowledgeTimer() => ClickClock(MouseButtons.Middle);

    // The clock in the vatSys menu bar does its actions when the mouse button is released
    void ClickClock(MouseButtons button)
    {
        Reflection.InvokeMethod(
            mainForm,
            "clockToolStripMenuItem_MouseUp",
            null,
            new MouseEventArgs(button, 1, 0, 0, 0));
    }
}
