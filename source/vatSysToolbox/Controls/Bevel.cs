using System.Windows;
using System.Windows.Media;

namespace vatSysToolbox.Controls;

/// <summary>
/// The brushes and the sizes for the beveled (3D) edges of the vatSys controls.
/// </summary>
public static class Bevel
{
    public static float Alpha = 0.4f;
    public static Brush LightBrush = new SolidColorBrush(Color.FromScRgb(Alpha, 255, 255, 255));
    public static Brush DarkBrush = new SolidColorBrush(Color.FromScRgb(Alpha, 0, 0, 0));

    public static Thickness BorderThickness = new(2);
    public static double LineWidth = 4;
}
