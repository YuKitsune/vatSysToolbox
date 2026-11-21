using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin.FlightPlan;

/// <summary>
/// Selects the Flight Plan Window that opens: the window of this plugin, or the vatSys window. The selection stays
/// for the next time that vatSys starts.
/// </summary>
/// <remarks>
/// <para>
/// A window that is open does not change. The selection applies to the next window that opens.
/// </para>
/// <para>
/// Make this class before the messenger starts, because it subscribes to its message. Use it on the UI thread.
/// </para>
/// </remarks>
public class FlightPlanWindowSwitch
{
    const string ToggleMessage = "dev.eoinmotherway.eurocatmenus.toggle-eurocat-flight-plan-window";
    const string OnEvent = "dev.eoinmotherway.eurocatmenus.eurocat-flight-plan-window-enabled";
    const string OffEvent = "dev.eoinmotherway.eurocatmenus.eurocat-flight-plan-window-disabled";

    readonly FlightPlanWindowSettings _settings;
    readonly MenuEvents _events;

    public FlightPlanWindowSwitch(
        FlightPlanWindowSettings settings,
        MenuEvents events,
        IPluginMessenger messenger,
        IGuiInvoker guiInvoker)
    {
        _settings = settings;
        _events = events;

        // The messenger receives the messages on a background thread
        messenger.Subscribe(ToggleMessage, (_, _) =>
        {
            guiInvoker.InvokeOnGUI(_ => Toggle());
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// <c>true</c> if the window of this plugin opens, and <c>false</c> if the vatSys window opens.
    /// </summary>
    public bool IsEurocatWindowEnabled => FlightPlanWindowPatches.IsEnabled;

    public event EventHandler? IsEurocatWindowEnabledChanged;

    /// <summary>
    /// Selects the other window.
    /// </summary>
    public void Toggle()
    {
        var isEnabled = !FlightPlanWindowPatches.IsEnabled;

        FlightPlanWindowPatches.IsEnabled = isEnabled;
        _settings.Save(isEnabled);

        IsEurocatWindowEnabledChanged?.Invoke(this, EventArgs.Empty);
        _events.Publish(isEnabled ? OnEvent : OffEvent);
    }

    /// <summary>
    /// Sends the event for the selection when the plugin starts, so that the menu shows it. Call this method after
    /// the items subscribe to the events.
    /// </summary>
    public void SendCurrentState()
    {
        _events.Publish(IsEurocatWindowEnabled ? OnEvent : OffEvent);
    }
}
