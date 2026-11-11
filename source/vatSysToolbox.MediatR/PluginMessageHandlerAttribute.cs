namespace vatSysToolbox.MediatR;

/// <summary>
/// Connects a MediatR handler to the messages of one type from other plugins.
/// Use it on an <see cref="global::MediatR.INotificationHandler{TNotification}"/> or an
/// <see cref="global::MediatR.IRequestHandler{TRequest,TResponse}"/>. Then call
/// <see cref="MediatRMessagingExtensionMethods.SubscribeHandlers"/>.
/// </summary>
/// <remarks>
/// MediatR publishes a notification to all the handlers of its class. Thus, all the handlers of the notification
/// receive the messages of this type, also the handlers that do not have this attribute.
/// </remarks>
/// <param name="type">The type of the message (for example, <c>dev.example.my-plugin.thing-changed</c>).</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginMessageHandlerAttribute(string type) : Attribute
{
    public string Type { get; } = type;
}
