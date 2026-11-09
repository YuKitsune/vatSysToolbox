using System.Diagnostics;

namespace vatSysToolbox.DesignTime;

static class DesignMode
{
    const string VatSysProcessName = "vatSys";

    /// <summary>
    /// <c>true</c> if the code does not run in vatSys (for example, in the XAML designer).
    /// </summary>
    /// <remarks>
    /// Some designers (for example, Rider) do not set <c>DesignerProperties.IsInDesignMode</c>.
    /// Thus, this property uses the name of the process.
    /// </remarks>
    public static bool IsActive { get; } = !string.Equals(
        Process.GetCurrentProcess().ProcessName,
        VatSysProcessName,
        StringComparison.OrdinalIgnoreCase);
}
