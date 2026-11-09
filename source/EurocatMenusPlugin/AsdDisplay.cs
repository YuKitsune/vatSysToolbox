using System.Windows.Forms;

namespace EurocatMenusPlugin;

/// <summary>
/// An air situation display (ASD) in vatSys.
/// </summary>
/// <remarks>
/// The vatSys ASD control is internal. Thus, this class uses reflection to get to its public members.
/// </remarks>
public class AsdDisplay(Control asd)
{
    // vatSys shows the range in four digits
    const int MinimumRange = 1;
    const int MaximumRange = 9999;

    /// <summary>
    /// The distance from the left side to the right side of the display, in nautical miles.
    /// </summary>
    public int Range
    {
        get
        {
            var range = (int)Reflection.InvokeMethod(asd, "GetRange")!;
            return Math.Min(MaximumRange, Math.Max(MinimumRange, range));
        }
    }

    /// <summary>
    /// The position status of the display: CEN, OFC, DEF, STO, or RST.
    /// </summary>
    public string Status => (string)Reflection.GetField(asd, "StatusLabelText")!;

    public void SetRange(int range)
    {
        // These are the arguments that the vatSys menu bar uses: an exact range, and a redraw, but no update.
        Reflection.InvokeMethod(asd, "SetZoom", (double)range, true, true, false);
    }
}
