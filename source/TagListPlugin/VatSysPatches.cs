using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using HarmonyLib;
using vatsys;
using vatSysToolbox;

namespace TagListPlugin;

/// <summary>
/// Harmony patches that send vatSys events (for example, <see cref="QuickTagAddedMessage"/>) with
/// <see cref="WeakReferenceMessenger.Default"/>. vatSys does not send events for these changes.
/// </summary>
/// <remarks>
/// The JIT compiler can inline small methods (for example, the setter of <see cref="QuickTag.LabelData"/>). Then the
/// patch does not run for that caller, and there is no event.
/// </remarks>
public static class VatSysPatches
{
    const string HarmonyId = "dev.eoinmotherway.tag-list";

    // vatSys can call the patched methods on any thread
    static IErrorReporter? _errorReporter;

    /// <exception cref="MissingMemberException">
    /// A vatSys member is not found (for example, after a vatSys update).
    /// </exception>
    public static void Apply(IErrorReporter errorReporter)
    {
        _errorReporter = errorReporter;

        var harmony = new Harmony(HarmonyId);

        harmony.Patch(
            FindMethod(typeof(RDP), nameof(RDP.AddQuickTag)),
            postfix: Patch(nameof(AddQuickTagPostfix)));

        // The Quick Tag window replaces the quick tag with a new quick tag. Then it copies the CFL and the global ops
        // text to the new quick tag. The setter patches send these changes.
        harmony.Patch(
            FindMethod(typeof(RDP), nameof(RDP.EditQuickTag)),
            postfix: Patch(nameof(EditQuickTagPostfix)));

        // Many parts of vatSys remove quick tags (for example, when a flight plan is coupled to the track)
        harmony.Patch(
            FindMethod(typeof(RDP), nameof(RDP.RemoveQuickTag)),
            postfix: Patch(nameof(RemoveQuickTagPostfix)));

        // The text field in the label, the Quick Tag window, and other plugins set the global ops text
        harmony.Patch(
            FindSetter(typeof(QuickTag), nameof(QuickTag.LabelData)),
            postfix: Patch(nameof(LabelDataPostfix)));

        harmony.Patch(FindSetter(typeof(QuickTag), nameof(QuickTag.CFL)), postfix: Patch(nameof(CflPostfix)));

        // vatSys sets the altitude at each radar update, also when it does not change
        harmony.Patch(
            FindSetter(typeof(RDP.RadarTrack), nameof(RDP.RadarTrack.CorrectedAltitude)),
            prefix: Patch(nameof(CorrectedAltitudePrefix)),
            postfix: Patch(nameof(CorrectedAltitudePostfix)));
    }

    // The names of the parameters must be the same as in the vatSys methods

    static void AddQuickTagPostfix(QuickTag qt)
    {
        // vatSys does not add the quick tag if a parameter is null
        if (qt is null)
            return;
        
        Send(new QuickTagAddedMessage(qt));
    }

    static void EditQuickTagPostfix(QuickTag oldqt, QuickTag newqt)
    {
        if (oldqt is null || newqt is null)
            return;

        Send(new QuickTagRemovedMessage(oldqt));
        Send(new QuickTagAddedMessage(newqt));
    }

    static void RemoveQuickTagPostfix(QuickTag qt)
    {
        if (qt is null)
            return;
        
        Send(new QuickTagRemovedMessage(qt));
    }

    static void LabelDataPostfix(QuickTag __instance)
    {
        Send(new QuickTagLabelDataChangedMessage(__instance));
    }

    static void CflPostfix(QuickTag __instance)
    {
        Send(new QuickTagCflChangedMessage(__instance));
    }

    // Keeps the altitude before the change, so that the postfix sends the event only when the altitude changes
    static void CorrectedAltitudePrefix(RDP.RadarTrack __instance, out int __state)
    {
        __state = __instance.CorrectedAltitude;
    }

    static void CorrectedAltitudePostfix(RDP.RadarTrack __instance, int __state)
    {
        if (__instance.CorrectedAltitude == __state)
            return;
        
        Send(new RadarTrackAltitudeChangedMessage(__instance));
    }

    // The messenger calls the recipients on this thread. An exception goes to the vatSys code that called the patched
    // method. Thus, report it here.
    static void Send<TMessage>(TMessage message)
        where TMessage : class
    {
        try
        {
            WeakReferenceMessenger.Default.Send(message);
        }
        catch (Exception ex)
        {
            _errorReporter?.ReportError(ex, $"Cannot send the vatSys event {typeof(TMessage).Name}");
        }
    }

    static HarmonyMethod Patch(string methodName) => new(typeof(VatSysPatches), methodName);

    static MethodInfo FindMethod(Type type, string methodName)
    {
        return AccessTools.Method(type, methodName) ?? throw new MissingMemberException(type.Name, methodName);
    }

    static MethodInfo FindSetter(Type type, string propertyName)
    {
        return AccessTools.PropertySetter(type, propertyName)
            ?? throw new MissingMemberException(type.Name, propertyName);
    }
}
