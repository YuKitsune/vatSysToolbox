using System.Text.Json;

namespace vatSysToolbox.Messaging;

/// <summary>
/// Converts message data to bytes, and bytes to message data, for one format.
/// To use a different format (for example, MessagePack), make a class that implements this interface.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// The format (for example, <c>application/json</c>).
    /// </summary>
    string ContentType { get; }

    byte[] Serialize<T>(T value);
    T Deserialize<T>(byte[] payload);
}

public class JsonMessageSerializer(JsonSerializerOptions? options = null) : IMessageSerializer
{
    public string ContentType => "application/json";

    public byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, options);
    }

    public T Deserialize<T>(byte[] payload)
    {
        return JsonSerializer.Deserialize<T>(payload, options)
            ?? throw new MessagingException($"The JSON payload for {typeof(T)} is null.");
    }
}
