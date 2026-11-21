using System.Reflection;
using System.Windows.Forms;
using HarmonyLib;
using vatsys;
using vatSysToolbox;

namespace EurocatMenusPlugin.FlightPlan;

/// <summary>
/// Harmony patches that show <see cref="FlightPlanWindow"/> in place of the vatSys Flight Plan Window. vatSys has no
/// extension point for this window.
/// </summary>
/// <remarks>
/// <para>
/// All vatSys code opens the window with <c>MMI.OpenFPWindow</c>, thus the first patch is a prefix on that method. But
/// that method is small, and the JIT compiler can inline small methods. Then the patch does not run for that caller.
/// </para>
/// <para>
/// Thus, the second patch is a prefix on the <c>Load</c> event handler of the vatSys window. vatSys calls an event
/// handler through a delegate, which the JIT compiler does not inline. This patch finds a vatSys window that the
/// first patch did not stop, and replaces it before it shows.
/// </para>
/// <para>
/// If <see cref="FlightPlanWindow"/> cannot open, the vatSys window opens. The vatSys window also opens while
/// <see cref="IsEnabled"/> is <c>false</c>.
/// </para>
/// </remarks>
public static class FlightPlanWindowPatches
{
    const string HarmonyId = "dev.eoinmotherway.eurocatmenus.flightplanwindow";

    // The vatSys window is internal
    const string VatSysWindowTypeName = "vatsys.FlightPlanWindow";
    const string VatSysWindowLoadMethod = "FlightPlanWindow_Load";
    const string VatSysWindowFdrField = "_fdr";

    const string OpenMethod = "OpenFPWindow";

    static IErrorReporter? _errorReporter;
    static Action<FDP2.FDR?>? _openWindow;
    static FieldInfo? _vatSysWindowFdrField;

    // True while the vatSys window opens because FlightPlanWindow could not open. vatSys opens the window on the UI
    // thread only.
    [ThreadStatic]
    static bool _allowVatSysWindow;

    // vatSys opens the window on the UI thread, and FlightPlanWindowSwitch changes this value on the UI thread. The
    // plugin sets the first value before vatSys starts its message loop.
    static volatile bool _isEnabled = true;

    /// <summary>
    /// If <c>false</c>, the patches do nothing, and the vatSys window opens. The default is <c>true</c>.
    /// </summary>
    public static bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>
    /// <c>true</c> if the second patch replaced the last vatSys window, and not the first patch.
    /// </summary>
    public static bool LastOpenReplacedVatSysWindow { get; private set; }

    /// <param name="errorReporter">Reports the exceptions that occur in the patches.</param>
    /// <param name="openWindow">
    /// Opens the window of this plugin for a flight plan (<c>null</c> for an empty window). vatSys calls it on the UI
    /// thread, and the window must be open when it returns (see <see cref="FlightPlanWindow.Open"/>).
    /// </param>
    /// <exception cref="MissingMemberException">
    /// A vatSys member is not found (for example, after a vatSys update).
    /// </exception>
    public static void Apply(IErrorReporter errorReporter, Action<FDP2.FDR?> openWindow)
    {
        _errorReporter = errorReporter;
        _openWindow = openWindow;

        var harmony = new Harmony(HarmonyId);

        // Find all members before the first patch, so that either all patches apply or none
        var openWithFdr = AccessTools.Method(typeof(MMI), OpenMethod, [typeof(FDP2.FDR)])
            ?? throw new MissingMemberException(nameof(MMI), OpenMethod);
        var openEmpty = AccessTools.Method(typeof(MMI), OpenMethod, Type.EmptyTypes)
            ?? throw new MissingMemberException(nameof(MMI), OpenMethod);

        var vatSysWindowType = typeof(MMI).Assembly.GetType(VatSysWindowTypeName)
            ?? throw new MissingMemberException(VatSysWindowTypeName);
        var vatSysWindowLoad = AccessTools.Method(vatSysWindowType, VatSysWindowLoadMethod)
            ?? throw new MissingMemberException(VatSysWindowTypeName, VatSysWindowLoadMethod);
        _vatSysWindowFdrField = AccessTools.Field(vatSysWindowType, VatSysWindowFdrField)
            ?? throw new MissingMemberException(VatSysWindowTypeName, VatSysWindowFdrField);

        harmony.Patch(openWithFdr, prefix: Patch(nameof(OpenWithFdrPrefix)));
        harmony.Patch(openEmpty, prefix: Patch(nameof(OpenEmptyPrefix)));
        harmony.Patch(vatSysWindowLoad, prefix: Patch(nameof(VatSysWindowLoadPrefix)));
    }

    // A prefix that returns false stops the vatSys method

    static bool OpenWithFdrPrefix(FDP2.FDR? fdr) => !TryOpen(fdr);

    static bool OpenEmptyPrefix() => !TryOpen(null);

    // Returns false if the window cannot open. Then the vatSys window opens.
    static bool TryOpen(FDP2.FDR? fdr)
    {
        if (!IsEnabled || _allowVatSysWindow)
            return false;

        try
        {
            LastOpenReplacedVatSysWindow = false;
            OpenWindow(fdr);
            return true;
        }
        catch (Exception ex)
        {
            // An exception that is not caught here goes to the vatSys code that opens the window
            ErrorReporter.ReportError(ex, "Cannot open the Flight Plan Window. The vatSys window opens.");
            _allowVatSysWindow = true;
            return false;
        }
    }

    static void VatSysWindowLoadPrefix(object __instance)
    {
        if (_allowVatSysWindow)
        {
            // FlightPlanWindow could not open, thus keep this vatSys window
            _allowVatSysWindow = false;
            return;
        }

        if (!IsEnabled || __instance is not Form vatSysWindow)
            return;

        try
        {
            var fdr = _vatSysWindowFdrField?.GetValue(vatSysWindow) as FDP2.FDR;
            OpenWindow(fdr);
            LastOpenReplacedVatSysWindow = true;

            // The vatSys window is in its Show call. Do not show it, and close it after that call returns.
            vatSysWindow.Opacity = 0;
            vatSysWindow.BeginInvoke(new Action(() =>
            {
                try
                {
                    vatSysWindow.Close();
                    vatSysWindow.Dispose();
                }
                catch (Exception ex)
                {
                    ErrorReporter.ReportError(ex, "Cannot close the vatSys Flight Plan Window");
                }
            }));
        }
        catch (Exception ex)
        {
            ErrorReporter.ReportError(ex, "Cannot open the Flight Plan Window. The vatSys window opens.");
        }
    }

    static IErrorReporter ErrorReporter => _errorReporter ?? NullErrorReporter.Instance;

    static void OpenWindow(FDP2.FDR? fdr)
    {
        var openWindow = _openWindow
            ?? throw new InvalidOperationException("The patches are not applied");
        openWindow(fdr);
    }

    static HarmonyMethod Patch(string methodName) => new(typeof(FlightPlanWindowPatches), methodName);
}
