using System.Windows;
using System.Windows.Media;
using vatsys;

namespace vatSysToolbox;

/// <summary>
/// The font that vatSys uses for its windows.
/// </summary>
public static class VatSysFont
{
    public static FontFamily Family { get; } = new(MMI.eurofont_winsml.FontFamily.Name);
    public static double Size { get; } = MMI.eurofont_winsml.Size;
    public static FontWeight Weight { get; } = MMI.eurofont_winsml.Bold ? FontWeights.Bold : FontWeights.Regular;
}
