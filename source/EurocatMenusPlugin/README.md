# Eurocat Menus Plugin

## Flight Plan Window

The plugin shows its own Flight Plan Window in place of the vatSys Flight Plan Window. It has the layout of the
Eurocat window, and it does the same vatSys actions as the vatSys window.

| Button | Action |
| --- | --- |
| CRE | Create a flight plan for the callsign in ACID. Press Enter to apply. |
| MOD | Modify the flight plan. Press Enter to apply. |
| EST | Activate the flight plan. This opens the vatSys estimate window. |
| BACK | Deactivate a coordinated flight plan. |
| DEP | Set or clear the ATD. An empty ATD sets the time now, or cancels the departure. |
| CNL | Cancel the flight plan. |
| ACC / HND | Accept jurisdiction / open the handoff menu. |
| INHI | Inhibit, or stop the inhibit. |
| PRL / CFL | Open the vatSys PRL and CFL menus. |
| NCOU | Decouple the radar track, and stop the automatic couple. |
| BCOU | Couple to the selected radar track, or start the automatic couple again. |
| RTE | Show or hide the graphic route on the ASD. |
| Clear / Escape | Clear the window / stop the current change. |

- Type in ASSR and OPDATA, and press Enter to apply them.
- REGN shows and changes the `REG/` item in OTHER.
- PSSR shows the SSR code that the coupled aircraft transmits.
- The title bar has the same buttons as the other vatSys windows: close, minimise, and send to back.

vatSys has no function for the other buttons and fields, thus they are not available. The runway and the SID or STAR
are not in this window. Set them from the label or the strip.

If the window cannot open (for example, after a vatSys update), the vatSys Flight Plan Window opens.

### Use the vatSys Flight Plan Window

To use the vatSys Flight Plan Window, remove the check mark from one of these items:

- **Set Up**, **Eurocat FPL Window** in the bottom menu.
- **Settings**, **Eurocat Flight Plan Window** in the vatSys menu bar.

Select the item again to use the window of the plugin. A window that is open does not change. The selection applies to
the next window that opens.

The plugin keeps the selection in `Documents\vatSys Files\EurocatMenus\Settings.json`, thus vatSys starts with the same
window the next time. The selection is the same for all profiles.

> [!NOTE]
> Even though the development of this plugin was inspired by real ATM systems such as those from Thales Group, the plugin is absolutely NOT affiliated with or endorsed by said company. The plugin and all associated files shall be used solely for the purpose of simulation on the VATSIM network.
