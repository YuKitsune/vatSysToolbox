using System.Text.Json.Nodes;
using vatsys;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin;

/// <summary>
/// Sends the built-in events for the state of vatSys and of this plugin: the strip sort mode and the vatSys menu bar.
/// </summary>
/// <remarks>
/// Make this class before the menus are added, so that it sends the event when the vatSys menu bar is hidden. Call
/// <see cref="SendCurrentState"/> after the items subscribe to the events.
/// </remarks>
public class BuiltInEvents
{
    // The payload is { "mode": "Alpha" }, { "mode": "Time" }, or { "mode": "Level" }
    const string StripSortModeChangedEvent = "dev.eoinmotherway.eurocatmenus.strip-sort-mode-changed";

    const string VatSysMenuShownEvent = "dev.eoinmotherway.eurocatmenus.default-menu-shown";
    const string VatSysMenuHiddenEvent = "dev.eoinmotherway.eurocatmenus.default-menu-hidden";

    // The vatSys event is internal. Thus, use reflection.
    const string StripSortModeChangedVatSysEvent = "StripSortModeChanged";

    readonly MenuEvents _events;

    public BuiltInEvents(MenuEvents events, MenuBarLayout menuBarLayout, IGuiInvoker guiInvoker)
    {
        _events = events;

        menuBarLayout.IsVatSysMenuVisibleChanged += (_, _) =>
            events.Raise(menuBarLayout.IsVatSysMenuVisible ? VatSysMenuShownEvent : VatSysMenuHiddenEvent);

        // vatSys changes the strip sort mode from the Strip Setup window, and when the position changes. Thus, send the
        // event on the UI thread.
        Reflection.AddStaticEventHandler(
            typeof(MMI),
            StripSortModeChangedVatSysEvent,
            (_, _) => guiInvoker.InvokeOnGUI(_ => SendStripSortMode()));
    }

    /// <summary>
    /// Sends the events for the state when the plugin starts. Then the menu shows the state of the profile, also if it
    /// is not the same as the default in the configuration.
    /// </summary>
    public void SendCurrentState()
    {
        SendStripSortMode();
    }

    void SendStripSortMode()
    {
        _events.Raise(StripSortModeChangedEvent, new JsonObject { ["mode"] = MMI.StripSortMode.ToString() });
    }
}
