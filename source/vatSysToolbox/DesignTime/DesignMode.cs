using System.ComponentModel;
using System.Windows;

namespace vatSysToolbox.DesignTime;

static class DesignMode
{
    /// <summary>
    /// <c>true</c> if the code runs in the XAML designer. In the designer, vatSys is not running.
    /// </summary>
    public static bool IsActive { get; } = DesignerProperties.GetIsInDesignMode(new DependencyObject());
}
