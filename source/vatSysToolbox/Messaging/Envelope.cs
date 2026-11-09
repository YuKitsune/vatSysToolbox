namespace vatSysToolbox.Messaging;

public enum EnvelopeKind
{
    Message,
    Request,
    Reply
}

/// <summary>
/// The data that a <see cref="IMessageTransport"/> moves between plugins.
/// </summary>
/// <param name="Id">The unique identifier of the envelope.</param>
/// <param name="Recipient">The name of the plugin that receives the envelope. If <c>null</c>, all plugins receive the envelope.</param>
/// <param name="CorrelationId">For a <see cref="EnvelopeKind.Reply"/>, the <paramref name="Id"/> of the request.</param>
/// <param name="Error">For a <see cref="EnvelopeKind.Reply"/>, the error message if the recipient could not handle the request.</param>
public sealed record Envelope(
    string Id,
    EnvelopeKind Kind,
    string Sender,
    string? Recipient,
    string Type,
    string ContentType,
    byte[] Payload,
    string? CorrelationId = null,
    string? Error = null);
