using System.Collections;
using System.Text;
using System.Text.Json.Nodes;
using EurocatMenusPlugin.Configuration;
using vatSysToolbox;
using vatSysToolbox.Messaging;

namespace EurocatMenusPlugin;

/// <summary>
/// The action of a button from the configuration that sends a message to other plugins.
/// </summary>
public class MessageButton
{
    const string JsonContentType = "application/json";

    readonly ButtonItemConfiguration _configuration;
    readonly IPluginMessenger _messenger;
    readonly IErrorReporter _errorReporter;
    readonly MessageContent _content;

    /// <exception cref="InvalidOperationException">The configuration has no message type.</exception>
    public MessageButton(ButtonItemConfiguration configuration, IPluginMessenger messenger, IErrorReporter errorReporter)
    {
        if (string.IsNullOrEmpty(configuration.MessageType))
            throw new InvalidOperationException($"The button {configuration.Text} has no message type");

        _configuration = configuration;
        _messenger = messenger;
        _errorReporter = errorReporter;
        _content = new MessageContent(
            JsonContentType,
            Encoding.UTF8.GetBytes(ToJson(configuration.Payload)?.ToJsonString() ?? "{}"));
    }

    public void Click()
    {
        _ = Send();
    }

    async Task Send()
    {
        try
        {
            if (_configuration.Recipient is null)
                await _messenger.Broadcast(_configuration.MessageType, _content);
            else
                await _messenger.Send(_configuration.Recipient, _configuration.MessageType, _content);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, $"Cannot send the message of the {_configuration.Text} button");
        }
    }

    // Converts the payload from the configuration file to JSON. The configuration file gives mappings as dictionaries,
    // sequences as lists, and scalars as strings, numbers, and booleans.
    static JsonNode? ToJson(object? value)
    {
        switch (value)
        {
            case null:
                return null;

            case IDictionary dictionary:
                var jsonObject = new JsonObject();
                foreach (DictionaryEntry entry in dictionary)
                    jsonObject[entry.Key.ToString()] = ToJson(entry.Value);
                return jsonObject;

            case IList list:
                var jsonArray = new JsonArray();
                foreach (var item in list)
                    jsonArray.Add(ToJson(item));
                return jsonArray;

            case bool boolean:
                return JsonValue.Create(boolean);
            case int number:
                return JsonValue.Create(number);
            case long number:
                return JsonValue.Create(number);
            case double number:
                return JsonValue.Create(number);
            case decimal number:
                return JsonValue.Create(number);

            default:
                return JsonValue.Create(value.ToString());
        }
    }
}
