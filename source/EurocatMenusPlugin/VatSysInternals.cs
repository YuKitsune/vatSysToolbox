using System.Windows.Forms;

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
