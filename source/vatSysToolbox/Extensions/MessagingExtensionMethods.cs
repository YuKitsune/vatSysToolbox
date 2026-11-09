using vatSysToolbox.Messaging;

namespace vatSysToolbox.Extensions;

public static class MessagingExtensionMethods
{
    public static MessageContent ToContent<T>(this IMessageSerializer serializer, T value)
    {
        return new MessageContent(serializer.ContentType, serializer.Serialize(value));
    }

    public static Task Send<T>(
        this IPluginMessenger messenger,
        string recipient,
        string type,
        T value,
        IMessageSerializer serializer,
        CancellationToken cancellationToken = default)
    {
        return messenger.Send(recipient, type, serializer.ToContent(value), cancellationToken);
    }

    public static Task Broadcast<T>(
        this IPluginMessenger messenger,
        string type,
        T value,
        IMessageSerializer serializer,
        CancellationToken cancellationToken = default)
    {
        return messenger.Broadcast(type, serializer.ToContent(value), cancellationToken);
    }

    public static Task<PluginMessage> Request<T>(
        this IPluginMessenger messenger,
        string recipient,
        string type,
        T value,
        IMessageSerializer serializer,
        CancellationToken cancellationToken = default)
    {
        return messenger.Request(recipient, type, serializer.ToContent(value), cancellationToken);
    }

    /// <summary>
    /// Converts the payload with the serializer that has the same <see cref="PluginMessage.ContentType"/>.
    /// </summary>
    /// <exception cref="MessagingException">No serializer has the content type of the message.</exception>
    public static T Deserialize<T>(this PluginMessage message, params IMessageSerializer[] serializers)
    {
        var serializer = serializers.FirstOrDefault(s => string.Equals(s.ContentType, message.ContentType, StringComparison.OrdinalIgnoreCase))
            ?? throw new MessagingException($"No serializer is available for the content type {message.ContentType}.");

        return serializer.Deserialize<T>(message.Payload);
    }
}
