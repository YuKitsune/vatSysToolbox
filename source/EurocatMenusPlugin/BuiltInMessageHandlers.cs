using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin;

/// <summary>
/// Handles the messages for the built-in actions of this plugin. The message buttons in the configuration and other
/// plugins send these messages to this plugin. The payload is not used.
/// </summary>
/// <remarks>
/// Make this class before the messenger starts.
/// </remarks>
public class BuiltInMessageHandlers
{
    const string OpenDisplaySettingsMessage = "dev.eoinmotherway.eurocatmenus.open-display-settings";
    const string OpenAudioSettingsMessage = "dev.eoinmotherway.eurocatmenus.open-audio-settings";
    const string OpenKeyboardSettingsMessage = "dev.eoinmotherway.eurocatmenus.open-keyboard-settings";
    const string OpenRestrictedAreaWindowMessage = "dev.eoinmotherway.eurocatmenus.open-restricted-area-window";
    const string ToggleDefaultMenuMessage = "dev.eoinmotherway.eurocatmenus.toggle-default-menu";
    const string OpenAirWindowMessage = "dev.eoinmotherway.eurocatmenus.open-air-window";
    const string OpenGroundWindowMessage = "dev.eoinmotherway.eurocatmenus.open-ground-window";
    const string OpenFlightPlanWindowMessage = "dev.eoinmotherway.eurocatmenus.open-flight-plan-window";
    const string ToggleLateralConflictToolMessage = "dev.eoinmotherway.eurocatmenus.toggle-lateral-conflict-tool";
    const string OpenSelcalWindowMessage = "dev.eoinmotherway.eurocatmenus.open-selcal-window";

    // The names of the vatSys menu items
    const string DisplaySettingsItem = "displayToolStripMenuItem1";
    const string AudioSettingsItem = "audioToolStripMenuItem";
    const string KeyboardSettingsItem = "keyToolStripMenuItem";
    const string RestrictedAreasItem = "restrictedAreasToolStripMenuItem";
    const string AirWindowItem = "secondASDToolStripMenuItem";
    const string GroundWindowItem = "ASMGCSToolStripMenuItem";
    const string FlightPlanItem = "flightPlanToolStripMenuItem";

    // Starts or stops the lateral conflict (LATC) tool for the selected track, the same as the LATC key (F2 by default)
    const string ConflictAreaItem = "latcToolStripMenuItem";

    // Opens the SELCAL window for the selected aircraft. vatSys does this only when it is connected as a controller.
    const string SelcalItem = "sELCALToolStripMenuItem";

    readonly IPluginMessenger _messenger;
    readonly IGuiInvoker _guiInvoker;

    public BuiltInMessageHandlers(
        VatSysMenu vatSysMenu,
        MenuBarLayout menuBarLayout,
        IPluginMessenger messenger,
        IGuiInvoker guiInvoker)
    {
        _messenger = messenger;
        _guiInvoker = guiInvoker;

        Handle(OpenDisplaySettingsMessage, () => vatSysMenu.Click(DisplaySettingsItem));
        Handle(OpenAudioSettingsMessage, () => vatSysMenu.Click(AudioSettingsItem));
        Handle(OpenKeyboardSettingsMessage, () => vatSysMenu.Click(KeyboardSettingsItem));
        Handle(OpenRestrictedAreaWindowMessage, () => vatSysMenu.Click(RestrictedAreasItem));
        Handle(ToggleDefaultMenuMessage, menuBarLayout.ToggleVatSysMenu);
        Handle(OpenAirWindowMessage, () => vatSysMenu.Click(AirWindowItem));
        Handle(OpenGroundWindowMessage, () => vatSysMenu.Click(GroundWindowItem));
        Handle(OpenFlightPlanWindowMessage, () => vatSysMenu.Click(FlightPlanItem));
        Handle(ToggleLateralConflictToolMessage, () => vatSysMenu.Click(ConflictAreaItem));
        Handle(OpenSelcalWindowMessage, () => vatSysMenu.Click(SelcalItem));
    }

    // The messenger receives the messages on a background thread. Thus, do the action on the UI thread. The GUI invoker
    // reports the errors.
    void Handle(string messageType, Action action)
    {
        _messenger.Subscribe(messageType, (_, _) =>
        {
            _guiInvoker.InvokeOnGUI(_ => action());
            return Task.CompletedTask;
        });
    }
}
