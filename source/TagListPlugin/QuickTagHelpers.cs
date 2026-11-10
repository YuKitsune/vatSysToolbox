using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using vatsys;

namespace TagListPlugin;

/// <summary>
/// Adds and removes the quick tag of the selected track, the same as Tools, Quick Tag in the vatSys menu bar.
/// </summary>
public static class QuickTagHelpers
{
    const string QuickTagWindowTypeName = "vatsys.QuickTagWindow";

    /// <summary>
    /// Opens the vatSys Quick Tag window for the selected track, the same as Tools, Quick Tag, Add. The window opens in
    /// the middle of the screen of the main form, and not at the mouse position. Does nothing if the selected track is
    /// not an uncoupled radar track.
    /// </summary>
    public static void OpenQuickTagWindow(Form mainForm)
    {
        // A quick tag is only for a radar track that is not coupled to a flight plan. vatSys uses the same rule to
        // enable Tools, Quick Tag.
        var selectedTrack = MMI.SelectedTrack;
        if (selectedTrack?.SourceData is not RDP.RadarTrack || selectedTrack.GetFDR() is not null)
            return;

        using var window = CreateQuickTagWindow(selectedTrack.GetRadarTrack());

        var screen = Screen.FromControl(mainForm).WorkingArea;
        window.StartPosition = FormStartPosition.Manual;
        window.Location = new Point(
            screen.Left + (screen.Width - window.Width) / 2,
            screen.Top + (screen.Height - window.Height) / 2);

        window.ShowDialog();
    }

    /// <summary>
    /// Removes the quick tag of the selected track, the same as Tools, Quick Tag, Remove.
    /// </summary>
    public static void RemoveSelectedQuickTag()
    {
        var selectedTrack = MMI.SelectedTrack;
        if (selectedTrack?.SourceData is not RDP.RadarTrack)
            return;

        RDP.RemoveQuickTag(selectedTrack.GetQuickTag());

        // vatSys does not change the state of the track when the quick tag is removed. Thus, do the same as vatSys and
        // set the state again, so that the track is not shown as jurisdiction.
        MMI.SetTrackState(selectedTrack);
    }

    // The Quick Tag window is internal in vatSys. Thus, use reflection.
    static Form CreateQuickTagWindow(RDP.RadarTrack radarTrack)
    {
        var type = typeof(MMI).Assembly.GetType(QuickTagWindowTypeName)
            ?? throw new TypeLoadException($"Cannot find the type {QuickTagWindowTypeName}");

        try
        {
            return (Form)Activator.CreateInstance(type, radarTrack);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Show the exception from vatSys, and not the exception from reflection
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
