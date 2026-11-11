using System.Reflection;
using System.Runtime.ExceptionServices;
using MediatR;
using vatSysToolbox.Extensions;
using vatSysToolbox.Messaging;

namespace vatSysToolbox.MediatR;

/// <summary>
/// Sends the messages that other plugins send to this plugin to MediatR handlers.
/// </summary>
/// <remarks>
/// The handlers run on the thread of the messenger, and not on the UI thread.
/// </remarks>
public static class MediatRMessagingExtensionMethods
{
    /// <summary>
    /// Publishes each message of the type as a notification. All the <see cref="INotificationHandler{TNotification}"/>
    /// handlers of the notification receive it.
    /// Dispose the return value to stop.
    /// </summary>
    /// <param name="type">The type of the message (for example, <c>dev.example.my-plugin.thing-changed</c>).</param>
    /// <param name="serializer">Converts the payload of the message to the notification.</param>
    public static IDisposable PublishNotifications<TNotification>(
        this IPluginMessenger messenger,
        string type,
        IMediator mediator,
        IMessageSerializer serializer)
        where TNotification : INotification
    {
        return messenger.Subscribe(type, (message, cancellationToken) =>
            mediator.Publish(message.Deserialize<TNotification>(serializer), cancellationToken));
    }

    /// <summary>
    /// Sends each request of the type to its <see cref="IRequestHandler{TRequest,TResponse}"/>, and sends the response
    /// back to the plugin that made the request.
    /// Dispose the return value to stop.
    /// </summary>
    /// <param name="type">The type of the request (for example, <c>dev.example.my-plugin.get-thing</c>).</param>
    /// <param name="serializer">Converts the payload to the request, and the response to a payload.</param>
    /// <exception cref="InvalidOperationException">The type already has a request handler.</exception>
    public static IDisposable SendRequests<TRequest, TResponse>(
        this IPluginMessenger messenger,
        string type,
        IMediator mediator,
        IMessageSerializer serializer)
        where TRequest : IRequest<TResponse>
    {
        return messenger.HandleRequests(type, async (message, cancellationToken) =>
        {
            var response = await mediator
                .Send(message.Deserialize<TRequest>(serializer), cancellationToken)
                .ConfigureAwait(false);

            return serializer.ToContent(response);
        });
    }

    /// <summary>
    /// Connects each handler in the assembly that has a <see cref="PluginMessageHandlerAttribute"/>. A notification
    /// handler is connected with <see cref="PublishNotifications{TNotification}"/>, and a request handler with
    /// <see cref="SendRequests{TRequest,TResponse}"/>.
    /// Dispose the return value to stop.
    /// </summary>
    /// <param name="serializer">Converts the payloads to the notifications and the requests, and the responses.</param>
    /// <exception cref="InvalidOperationException">
    /// A handler with the attribute is not one notification handler or one request handler, or two request handlers
    /// have the same message type.
    /// </exception>
    public static IDisposable SubscribeHandlers(
        this IPluginMessenger messenger,
        Assembly assembly,
        IMediator mediator,
        IMessageSerializer serializer)
    {
        var subscriptions = new List<IDisposable>();

        // MediatR publishes a notification to all its handlers. Connect each message type and notification only once,
        // so that a handler does not receive a message two times.
        var connected = new HashSet<(string MessageType, Type MediatRType)>();

        try
        {
            foreach (var handlerType in assembly.GetTypes())
            {
                var attribute = handlerType.GetCustomAttribute<PluginMessageHandlerAttribute>();
                if (attribute is null)
                    continue;

                var handlerInterface = GetHandlerInterface(handlerType);
                var typeArguments = handlerInterface.GetGenericArguments();
                if (!connected.Add((attribute.Type, typeArguments[0])))
                    continue;

                var method = handlerInterface.GetGenericTypeDefinition() == typeof(INotificationHandler<>)
                    ? nameof(PublishNotifications)
                    : nameof(SendRequests);

                subscriptions.Add(Connect(method, typeArguments, messenger, attribute.Type, mediator, serializer));
            }
        }
        catch
        {
            // Do not keep some of the handlers connected
            foreach (var subscription in subscriptions)
                subscription.Dispose();

            throw;
        }

        return new Subscriptions(subscriptions);
    }

    static Type GetHandlerInterface(Type handlerType)
    {
        var handlerInterfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Where(i => i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)
                || i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .ToList();

        if (handlerInterfaces.Count != 1)
            throw new InvalidOperationException(
                $"{handlerType} has a {nameof(PluginMessageHandlerAttribute)}. It must implement one " +
                "INotificationHandler<TNotification> or one IRequestHandler<TRequest, TResponse>.");

        return handlerInterfaces[0];
    }

    // The handler types are known only at runtime. Thus, call the generic method with reflection.
    static IDisposable Connect(
        string methodName,
        Type[] typeArguments,
        IPluginMessenger messenger,
        string messageType,
        IMediator mediator,
        IMessageSerializer serializer)
    {
        var method = typeof(MediatRMessagingExtensionMethods)
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(typeArguments);

        try
        {
            return (IDisposable)method.Invoke(null, [messenger, messageType, mediator, serializer])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Show the exception from the method, and not the exception from reflection
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    sealed class Subscriptions(List<IDisposable> subscriptions) : IDisposable
    {
        public void Dispose()
        {
            foreach (var subscription in subscriptions)
                subscription.Dispose();

            subscriptions.Clear();
        }
    }
}
