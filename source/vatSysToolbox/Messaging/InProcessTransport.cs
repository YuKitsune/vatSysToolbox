using System.Collections.Concurrent;
using SharedEnvelope = System.Collections.Generic.IDictionary<string, object>;
using SharedMailbox = System.Tuple<
    System.Collections.Concurrent.ConcurrentQueue<System.Collections.Generic.IDictionary<string, object>>,
    System.Threading.SemaphoreSlim>;

namespace vatSysToolbox.Messaging;

/// <summary>
/// Moves envelopes between the plugins in the same vatSys process. The envelopes stay in memory.
/// </summary>
/// <remarks>
/// <para>
/// Each plugin can contain its own copy of the toolbox (for example, with ILMerge), and the copies can have different
/// versions. The CLR thinks that the types in each copy are different types. Thus, the state that the plugins share
/// contains only .NET Framework types, and it is kept in the data slots of the AppDomain.
/// </para>
/// <para>
/// Do not change the shared state contract (the slot names, the types, and the envelope keys).
/// If you must change it, use new slot names (for example, <c>v2</c>).
/// You can add new envelope keys. Older copies ignore the keys that they do not know.
/// </para>
/// </remarks>
public class InProcessTransport : IMessageTransport
{
    // Value: ConcurrentDictionary<string, SharedMailbox>. The key is the plugin name.
    const string MailboxesSlotName = "vatSysToolbox.Messaging.InProcess.v1.Mailboxes";

    // Value: ConcurrentDictionary<string, bool>. The keys are the names of the open mailboxes. The values are not used.
    const string OpenMailboxesSlotName = "vatSysToolbox.Messaging.InProcess.v1.OpenMailboxes";

    // The values are strings, except PayloadKey, which is a byte array.
    const string IdKey = "Id";
    const string KindKey = "Kind";
    const string SenderKey = "Sender";
    const string RecipientKey = "Recipient";
    const string TypeKey = "Type";
    const string ContentTypeKey = "ContentType";
    const string PayloadKey = "Payload";
    const string CorrelationIdKey = "CorrelationId";
    const string ErrorKey = "Error";

    readonly ConcurrentDictionary<string, SharedMailbox> _mailboxes = GetOrCreateSlot<ConcurrentDictionary<string, SharedMailbox>>(MailboxesSlotName);
    readonly ConcurrentDictionary<string, bool> _openMailboxes = GetOrCreateSlot<ConcurrentDictionary<string, bool>>(OpenMailboxesSlotName);

    public IMailbox OpenMailbox(string pluginName)
    {
        if (!_openMailboxes.TryAdd(pluginName, true))
            throw new MessagingException($"The plugin name {pluginName} is already in use.");

        return new Mailbox(pluginName, GetOrCreateMailbox(pluginName), () => _openMailboxes.TryRemove(pluginName, out _));
    }

    public Task Send(Envelope envelope, CancellationToken cancellationToken)
    {
        if (envelope.Recipient is not null)
        {
            Enqueue(envelope.Recipient, envelope);
            return Task.CompletedTask;
        }

        foreach (var pluginName in _openMailboxes.Keys)
        {
            if (pluginName != envelope.Sender)
                Enqueue(pluginName, envelope);
        }

        return Task.CompletedTask;
    }

    void Enqueue(string pluginName, Envelope envelope)
    {
        var mailbox = GetOrCreateMailbox(pluginName);
        mailbox.Item1.Enqueue(ToSharedEnvelope(envelope));
        mailbox.Item2.Release();
    }

    SharedMailbox GetOrCreateMailbox(string pluginName)
    {
        return _mailboxes.GetOrAdd(
            pluginName,
            _ => Tuple.Create(new ConcurrentQueue<SharedEnvelope>(), new SemaphoreSlim(0)));
    }

    static T GetOrCreateSlot<T>(string slotName) where T : class, new()
    {
        // All the copies of the toolbox get the same instance of an interned string.
        // Thus, all the copies use the same lock.
        lock (string.Intern(slotName))
        {
            var value = AppDomain.CurrentDomain.GetData(slotName);
            if (value is null)
            {
                value = new T();
                AppDomain.CurrentDomain.SetData(slotName, value);
            }

            return value as T ?? throw new MessagingException(
                $"The AppDomain slot {slotName} contains a {value.GetType()}. The expected type is {typeof(T)}.");
        }
    }

    static SharedEnvelope ToSharedEnvelope(Envelope envelope)
    {
        // Copy the payload, so that a change to the array by the sender or by a recipient does not change the data
        // that other plugins receive.
        var sharedEnvelope = new Dictionary<string, object>
        {
            [IdKey] = envelope.Id,
            [KindKey] = envelope.Kind.ToString(),
            [SenderKey] = envelope.Sender,
            [TypeKey] = envelope.Type,
            [ContentTypeKey] = envelope.ContentType,
            [PayloadKey] = envelope.Payload.ToArray()
        };

        if (envelope.Recipient is not null)
            sharedEnvelope[RecipientKey] = envelope.Recipient;

        if (envelope.CorrelationId is not null)
            sharedEnvelope[CorrelationIdKey] = envelope.CorrelationId;

        if (envelope.Error is not null)
            sharedEnvelope[ErrorKey] = envelope.Error;

        return sharedEnvelope;
    }

    static Envelope ToEnvelope(SharedEnvelope sharedEnvelope)
    {
        var kindName = GetRequiredValue<string>(sharedEnvelope, KindKey);
        if (!Enum.TryParse<EnvelopeKind>(kindName, out var kind))
            throw new MessagingException($"The envelope kind {kindName} is not known.");

        return new Envelope(
            GetRequiredValue<string>(sharedEnvelope, IdKey),
            kind,
            GetRequiredValue<string>(sharedEnvelope, SenderKey),
            GetOptionalValue<string>(sharedEnvelope, RecipientKey),
            GetRequiredValue<string>(sharedEnvelope, TypeKey),
            GetRequiredValue<string>(sharedEnvelope, ContentTypeKey),
            GetRequiredValue<byte[]>(sharedEnvelope, PayloadKey),
            GetOptionalValue<string>(sharedEnvelope, CorrelationIdKey),
            GetOptionalValue<string>(sharedEnvelope, ErrorKey));
    }

    static T GetRequiredValue<T>(SharedEnvelope sharedEnvelope, string key) where T : class
    {
        return GetOptionalValue<T>(sharedEnvelope, key)
            ?? throw new MessagingException($"The envelope does not contain the value {key}.");
    }

    static T? GetOptionalValue<T>(SharedEnvelope sharedEnvelope, string key) where T : class
    {
        if (!sharedEnvelope.TryGetValue(key, out var value))
            return null;

        return value as T ?? throw new MessagingException(
            $"The envelope value {key} is a {value.GetType()}. The expected type is {typeof(T)}.");
    }

    class Mailbox(string pluginName, SharedMailbox sharedMailbox, Action close) : IMailbox
    {
        Action? _close = close;

        public string PluginName => pluginName;

        public async Task<Envelope> Receive(CancellationToken cancellationToken)
        {
            // The semaphore count is the same as the number of envelopes in the queue.
            await sharedMailbox.Item2.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!sharedMailbox.Item1.TryDequeue(out var sharedEnvelope))
                throw new MessagingException($"The mailbox for {pluginName} is empty.");

            return ToEnvelope(sharedEnvelope);
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _close, null)?.Invoke();
        }
    }
}
