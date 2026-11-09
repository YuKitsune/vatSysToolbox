namespace vatSysToolbox.Messaging;

/// <summary>
/// The data of a message, and the format of the data.
/// </summary>
/// <param name="ContentType">The format of the payload (for example, <c>application/json</c>).</param>
public sealed record MessageContent(string ContentType, byte[] Payload);

/// <summary>
/// A message that a plugin received from a different plugin.
/// </summary>
/// <param name="Sender">The name of the plugin that sent the message.</param>
/// <param name="Type">The type of the message (for example, <c>dev.eoinmotherway.maestro.sequence-updated</c>).</param>
/// <param name="ContentType">The format of the payload (for example, <c>application/json</c>).</param>
public sealed record PluginMessage(string Sender, string Type, string ContentType, byte[] Payload);
