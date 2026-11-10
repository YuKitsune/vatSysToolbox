using System.Text;
using System.Text.Json.Nodes;
using vatSysToolbox;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin;

/// <summary>
/// The events that change the items of the menu (for example, they turn the toggle buttons on and off). The menu raises
/// the built-in events (for example, <c>dev.eoinmotherway.eurocatmenus.strip-setup-window-opened</c>). A message from
/// another plugin raises the event that has the same name as the type of the message.
/// </summary>
/// <remarks>
/// An event can have a JSON payload. For a message, the payload is the payload of the message.
/// Use this class on the UI thread. The handlers also run on the UI thread.
/// </remarks>
public class MenuEvents(IPluginMessenger messenger, IGuiInvoker guiInvoker, IErrorReporter errorReporter)
{
    const string JsonContentType = "application/json";

    readonly Dictionary<string, List<Action<JsonNode?>>> _handlers = new();

    /// <summary>
    /// Adds a handler for an event that does not use the payload. Add the handlers before the messenger starts, so that
    /// no message is lost.
    /// </summary>
    public void Subscribe(string name, Action handler) => Subscribe(name, _ => handler());

    /// <summary>
    /// Adds a handler for an event. The handler gets the payload, or <c>null</c> if the event has no JSON payload. Add
    /// the handlers before the messenger starts, so that no message is lost.
    /// </summary>
    public void Subscribe(string name, Action<JsonNode?> handler)
    {
        if (!_handlers.TryGetValue(name, out var handlers))
        {
            handlers = [];
            _handlers[name] = handlers;

            // The messenger receives the messages on a background thread. If the payload is not correct JSON, the
            // messenger reports the error.
            messenger.Subscribe(name, (message, _) =>
            {
                var payload = message.ContentType == JsonContentType && message.Payload.Length > 0
                    ? JsonNode.Parse(message.Payload)
                    : null;

                guiInvoker.InvokeOnGUI(_ => Raise(name, payload));
                return Task.CompletedTask;
            });
        }

        handlers.Add(handler);
    }

    public void Raise(string name, JsonNode? payload = null)
    {
        if (!_handlers.TryGetValue(name, out var handlers))
            return;

        foreach (var handler in handlers)
            handler(payload);
    }

    /// <summary>
    /// Raises the event in this plugin, and sends it to all the other plugins. A plugin does not receive its own
    /// messages, thus this plugin raises the event itself.
    /// </summary>
    public void Publish(string name, JsonNode? payload = null)
    {
        Raise(name, payload);
        _ = Broadcast(name, payload);
    }

    async Task Broadcast(string name, JsonNode? payload)
    {
        try
        {
            var content = new MessageContent(
                JsonContentType,
                Encoding.UTF8.GetBytes(payload?.ToJsonString() ?? "{}"));
            await messenger.Broadcast(name, content);
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, $"Cannot send the event {name}");
        }
    }
}
