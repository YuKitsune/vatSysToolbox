using System.Runtime.CompilerServices;
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
        : GetVatSysFamily();

    public static double Size { get; } = DesignMode.IsActive
        ? 16
        : GetVatSysSize();

    public static FontWeight Weight { get; } = DesignMode.IsActive || IsVatSysFontBold()
        ? FontWeights.Bold
        : FontWeights.Regular;

    // The JIT compiler loads MMI when it compiles the method that uses it. The designer cannot load MMI.
    // These methods are not inlined. Thus, MMI loads only when these methods run, and not in design mode.

    [MethodImpl(MethodImplOptions.NoInlining)]
    static FontFamily GetVatSysFamily() => new(MMI.eurofont_winsml.FontFamily.Name);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double GetVatSysSize() => MMI.eurofont_winsml.Size;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsVatSysFontBold() => MMI.eurofont_winsml.Bold;
}
