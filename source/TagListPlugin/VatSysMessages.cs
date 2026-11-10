using vatsys;

namespace TagListPlugin;

// The vatSys events that VatSysPatches sends with the weak reference messenger. vatSys sends them on its own thread.

/// <summary>
/// vatSys added a quick tag.
/// </summary>
public class QuickTagAddedMessage(QuickTag quickTag)
{
    public QuickTag QuickTag { get; } = quickTag;
}

/// <summary>
/// vatSys removed a quick tag.
/// </summary>
public class QuickTagRemovedMessage(QuickTag quickTag)
{
    public QuickTag QuickTag { get; } = quickTag;
}

/// <summary>
/// The global ops text of a quick tag changed.
/// </summary>
public class QuickTagLabelDataChangedMessage(QuickTag quickTag)
{
    public QuickTag QuickTag { get; } = quickTag;
}

/// <summary>
/// The CFL of a quick tag changed.
/// </summary>
public class QuickTagCflChangedMessage(QuickTag quickTag)
{
    public QuickTag QuickTag { get; } = quickTag;
}

/// <summary>
/// The altitude of a radar track changed.
/// </summary>
public class RadarTrackAltitudeChangedMessage(RDP.RadarTrack radarTrack)
{
    public RDP.RadarTrack RadarTrack { get; } = radarTrack;
}
