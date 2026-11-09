namespace vatSysToolbox.Messaging;

/// <summary>
/// Moves envelopes between plugins.
/// </summary>
public interface IMessageTransport
{
    /// <summary>
    /// Opens the inbound queue of a plugin. Only one plugin can open a queue with a given name.
    /// </summary>
    /// <exception cref="MessagingException">The queue is already open.</exception>
    IMailbox OpenMailbox(string pluginName);

    /// <summary>
    /// Adds the envelope to the inbound queue of <see cref="Envelope.Recipient"/>. If the recipient did not open its
    /// queue yet, the envelope stays in the queue until the recipient opens it.
    /// If <see cref="Envelope.Recipient"/> is <c>null</c>, adds the envelope to the queue of each plugin that has an
    /// open queue, except the sender.
    /// </summary>
    Task Send(Envelope envelope, CancellationToken cancellationToken);
}

/// <summary>
/// The inbound queue of a plugin. Dispose the mailbox to close the queue.
/// </summary>
public interface IMailbox : IDisposable
{
    string PluginName { get; }

    /// <summary>
    /// Waits for the next envelope, and removes it from the queue.
    /// </summary>
    Task<Envelope> Receive(CancellationToken cancellationToken);
}
