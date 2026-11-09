using System.Drawing;
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
    const string QuickTagWindowTypeName = "vatsys.QuickTagWindow";

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
    /// The ASD in the first Air window that is open, or <c>null</c> if no Air window is open.
    /// </summary>
    public AsdDisplay? FindSecondAsd()
    {
        var airWindow = Application.OpenForms
            .Cast<Form>()
            .FirstOrDefault(form => form.GetType().Name == AirWindowTypeName);

        return airWindow is null
            ? null
            : new AsdDisplay((Control)Reflection.GetField(airWindow, AsdControlName)!);
    }

    /// <summary>
    /// Opens the vatSys Quick Tag window for the selected track, the same as Tools, Quick Tag, Add. The window opens in
    /// the middle of the screen of the main form, and not at the mouse position. Does nothing if the selected track is
    /// not an uncoupled radar track.
    /// </summary>
    public void OpenQuickTagWindow()
    {
        // A quick tag is only for a radar track that is not coupled to a flight plan. vatSys uses the same rule to
        // enable Tools, Quick Tag.
        var selectedTrack = MMI.SelectedTrack;
        if (selectedTrack?.SourceData is not RDP.RadarTrack || selectedTrack.GetFDR() is not null)
            return;

        using var window = (Form)Reflection.CreateInstance(
            typeof(MMI).Assembly,
            QuickTagWindowTypeName,
            selectedTrack.GetRadarTrack());

        var screen = Screen.FromControl(mainForm).WorkingArea;
        window.StartPosition = FormStartPosition.Manual;
        window.Location = new Point(
            screen.Left + (screen.Width - window.Width) / 2,
            screen.Top + (screen.Height - window.Height) / 2);

        window.ShowDialog();
    }

    /// <summary>
    /// Opens the vatSys Time menu at the mouse position, the same as a click on the clock in the vatSys menu bar.
    /// </summary>
    public void OpenTimeMenu()
    {
        Reflection.InvokeMethod(
            mainForm,
            "clockToolStripMenuItem_MouseUp",
            null,
            new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
    }
}
