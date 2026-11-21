using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using vatsys;

namespace EurocatMenusPlugin.FlightPlan;

/// <summary>
/// The vatSys members that the vatSys Flight Plan Window uses, but that vatSys does not make public.
/// </summary>
/// <remarks>
/// These members are private or internal in vatSys. Thus, this class uses reflection. A vatSys update can change these
/// members. Then these methods throw an exception that gives the name of the member, and only that function of the
/// window stops.
/// </remarks>
static class FlightPlanInternals
{
    const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    // The key of the vatSys main form in Application.OpenForms
    const string VatSysMainFormKey = "MainForm";

    const string EstimateWindowTypeName = "vatsys.ESTWindow";
    const string ConfirmationWindowTypeName = "vatsys.GenericConfirmationWindow";
    const string NoFlightPlanEventName = "NoFPExists";

    static readonly Assembly VatSysAssembly = typeof(MMI).Assembly;

    /// <summary>
    /// The vatSys main form, or <c>null</c> if it is not open.
    /// </summary>
    public static Form? MainForm => Application.OpenForms[VatSysMainFormKey];

    /// <summary>
    /// Tells the network that the flight plan changed.
    /// </summary>
    public static void SendFlightPlanChange(FDP2.FDR fdr) => InvokeNetwork("SendFlightPlanChange", fdr);

    /// <summary>
    /// Tells the network about a flight plan that this controller made.
    /// </summary>
    public static void SendNewFlightPlan(FDP2.FDR fdr) => InvokeNetwork("SendNewFlightPlan", fdr);

    /// <summary>
    /// Asks the network for the flight plan of the callsign.
    /// </summary>
    public static void SendFlightPlanRequest(string callsign) => InvokeNetwork("SendFPRequest", callsign);

    /// <summary>
    /// Tells the network that the estimate for the position is cancelled.
    /// </summary>
    public static void SendCancelEstimate(FDP2.FDR fdr, FDP2.FDR.ExtractedRoute.Segment position) =>
        InvokeNetwork("SendCancelEST", fdr, position, ClientQueryEstimateType.ETO);

    /// <summary>
    /// Calls <paramref name="handler"/> with the callsign when the network has no flight plan for a callsign.
    /// vatSys can raise the event on any thread.
    /// </summary>
    /// <returns>An action that removes the handler.</returns>
    public static Action SubscribeNoFlightPlan(Action<string?> handler)
    {
        var eventInfo = typeof(Network).GetEvent(NoFlightPlanEventName, StaticFlags)
            ?? throw new MissingMemberException(nameof(Network), NoFlightPlanEventName);
        var addMethod = eventInfo.GetAddMethod(nonPublic: true)
            ?? throw new MissingMemberException(nameof(Network), NoFlightPlanEventName);
        var removeMethod = eventInfo.GetRemoveMethod(nonPublic: true)
            ?? throw new MissingMemberException(nameof(Network), NoFlightPlanEventName);

        // The event arguments type is internal. A method that takes EventArgs can handle the event, because delegate
        // parameters are contravariant.
        var relay = new NoFlightPlanRelay(handler);
        var relayMethod = typeof(NoFlightPlanRelay).GetMethod(nameof(NoFlightPlanRelay.Handle))!;
        var relayDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, relay, relayMethod);

        addMethod.Invoke(null, [relayDelegate]);
        return () => removeMethod.Invoke(null, [relayDelegate]);
    }

    /// <summary>
    /// Shows the vatSys window that sets the estimate and activates the flight plan.
    /// </summary>
    public static void ShowEstimateWindow(FDP2.FDR fdr)
    {
        var type = FindType(EstimateWindowTypeName);

        try
        {
            using var window = (Form)Activator.CreateInstance(type, InstanceFlags, null, [fdr], null);
            window.ShowDialog();
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            Rethrow(ex.InnerException);
        }
    }

    /// <summary>
    /// Shows the vatSys confirmation window, and returns <c>true</c> if the user selects Yes.
    /// </summary>
    public static bool Confirm(string title, string message)
    {
        var type = FindType(ConfirmationWindowTypeName);
        var show = type.GetMethod("Show", StaticFlags, null, [typeof(string), typeof(string)], null)
            ?? throw new MissingMemberException(type.Name, "Show");

        try
        {
            return (DialogResult)show.Invoke(null, [title, message]) == DialogResult.Yes;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            Rethrow(ex.InnerException);
            throw;
        }
    }

    static void InvokeNetwork(string methodName, params object[] arguments)
    {
        var instanceField = typeof(Network).GetField("Instance", StaticFlags)
            ?? throw new MissingMemberException(nameof(Network), "Instance");
        var network = instanceField.GetValue(null)
            ?? throw new InvalidOperationException("The vatSys network is not available");

        var argumentTypes = arguments.Select(argument => argument.GetType()).ToArray();
        var method = typeof(Network).GetMethod(methodName, InstanceFlags, null, argumentTypes, null)
            ?? throw new MissingMemberException(nameof(Network), methodName);

        try
        {
            method.Invoke(network, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            Rethrow(ex.InnerException);
        }
    }

    static Type FindType(string typeName)
    {
        return VatSysAssembly.GetType(typeName) ?? throw new MissingMemberException(typeName);
    }

    // Show the exception from vatSys, and not the exception from reflection
    static void Rethrow(Exception exception) => ExceptionDispatchInfo.Capture(exception).Throw();

    sealed class NoFlightPlanRelay(Action<string?> handler)
    {
        public void Handle(object sender, EventArgs e)
        {
            // The callsign is a field or a property of the internal event arguments type
            var type = e.GetType();
            var callsign = type.GetProperty("Callsign", InstanceFlags)?.GetValue(e)
                ?? type.GetField("Callsign", InstanceFlags)?.GetValue(e);

            handler(callsign as string);
        }
    }
}
