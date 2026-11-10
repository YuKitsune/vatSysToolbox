using System.Text.Json.Serialization;
using System.Windows.Forms;
using vatSysToolbox.Extensions;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin.Shims;

/// <summary>
/// Handles the messages of the Maestro plugin, until the Maestro plugin does this itself. The shim opens the TFMS
/// windows with the Maestro menu items.
/// </summary>
/// <remarks>
/// Make the shim before the messenger starts.
/// </remarks>
public class MaestroShim
{
    const string OpenTfmsWindowMessage = "dev.eoinmotherway.maestro.open-tfms-window";

    // The Maestro plugin has an item in this menu for each airport. The text of the item is the ICAO code.
    const string TfmsCategory = "TFMS";

    static readonly IMessageSerializer Serializer = new JsonMessageSerializer();

    readonly VatSysMenu _vatSysMenu;

    public MaestroShim(VatSysMenu vatSysMenu, IPluginMessenger messenger, IGuiInvoker guiInvoker)
    {
        _vatSysMenu = vatSysMenu;

        // The messenger receives the messages on a background thread
        messenger.Subscribe(OpenTfmsWindowMessage, (message, _) =>
        {
            var icao = message.Deserialize<OpenTfmsWindowPayload>(Serializer).Icao;
            guiInvoker.InvokeOnGUI(_ => OpenTfmsWindow(icao));
            return Task.CompletedTask;
        });
    }

    void OpenTfmsWindow(string icao)
    {
        var item = _vatSysMenu.FindPluginCategory(TfmsCategory)?.DropDownItems
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(menuItem => menuItem.Text == icao)
            ?? throw new InvalidOperationException(
                $"Cannot open the TFMS window for {icao}. The Maestro plugin is not loaded, or it has no configuration for {icao}.");

        item.PerformClick();
    }

    public class OpenTfmsWindowPayload
    {
        /// <summary>
        /// The ICAO code of the airport (for example, <c>YSSY</c>).
        /// </summary>
        [JsonPropertyName("icao")]
        public string Icao { get; set; } = string.Empty;
    }
}
