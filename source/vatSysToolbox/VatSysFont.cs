using System.Windows;
using System.Windows.Media;
using vatsys;
using vatSysToolbox.DesignTime;

namespace vatSysToolbox;

/// <summary>
/// The font that vatSys uses for its windows.
/// In the XAML designer, vatSys is not running. Thus, the designer uses the same values that vatSys sets for this font.
/// If Terminus is not installed, the designer uses Consolas.
/// </summary>
public static class VatSysFont
{
    public static FontFamily Family { get; } = DesignMode.IsActive
        ? new FontFamily("Terminus (TTF), Consolas")
        : new FontFamily(MMI.eurofont_winsml.FontFamily.Name);

    public static double Size { get; } = DesignMode.IsActive
        ? 16
        : MMI.eurofont_winsml.Size;

    public static FontWeight Weight { get; } = DesignMode.IsActive || MMI.eurofont_winsml.Bold
        ? FontWeights.Bold
        : FontWeights.Regular;
}
