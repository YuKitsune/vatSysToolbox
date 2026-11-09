using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace vatSysToolbox.Extensions;

/// <summary>
/// Registers the toolbox services. These methods do not replace a service that is already registered.
/// Thus, to use a different implementation, register it before you call these methods.
/// </summary>
/// <remarks>
/// The services use <see cref="ILogger{TCategoryName}"/>. You must register the logging services
/// (for example, with <c>AddLogging</c>).
/// </remarks>
public static class ServiceCollectionExtensionMethods
{
    /// <summary>
    /// Registers all the toolbox services.
    /// </summary>
    /// <param name="pluginName">
    /// The name that shows with the errors in the vatSys error window. Other plugins also use this name to send
    /// messages to the plugin.
    /// </param>
    public static IServiceCollection AddVatSysToolbox(this IServiceCollection services, string pluginName)
    {
        return services
            .AddClock()
            .AddErrorReporter(pluginName)
            .AddWindowManager()
            .AddWorkQueue()
            .AddPluginMessenger(pluginName);
    }

    /// <summary>
    /// Registers <see cref="IClock"/>.
    /// </summary>
    public static IServiceCollection AddClock(this IServiceCollection services)
    {
        services.TryAddSingleton<IClock, SystemClock>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IErrorReporter"/> and <see cref="IClock"/>.
    /// </summary>
    /// <param name="pluginName">The name that shows with the errors in the vatSys error window.</param>
    public static IServiceCollection AddErrorReporter(this IServiceCollection services, string pluginName)
    {
        services.AddClock();
        services.TryAddSingleton<IErrorReporter>(serviceProvider => new ErrorReporter(
            pluginName,
            serviceProvider.GetRequiredService<ILogger<ErrorReporter>>(),
            serviceProvider.GetRequiredService<IClock>()));

        return services;
    }

    /// <summary>
    /// Registers <see cref="WindowManager"/> and <see cref="IGuiInvoker"/>.
    /// You must also register <see cref="IErrorReporter"/>.
    /// </summary>
    public static IServiceCollection AddWindowManager(this IServiceCollection services)
    {
        services.TryAddSingleton<IGuiInvoker, GuiInvoker>();
        services.TryAddSingleton<WindowManager>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="WorkQueue"/> and <see cref="IClock"/>.
    /// You must also register <see cref="IErrorReporter"/>.
    /// </summary>
    public static IServiceCollection AddWorkQueue(this IServiceCollection services)
    {
        services.AddClock();
        services.TryAddSingleton<WorkQueue>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IPluginMessenger"/> and <see cref="IMessageTransport"/>.
    /// You must also register <see cref="IErrorReporter"/>.
    /// </summary>
    /// <param name="pluginName">The name that other plugins use to send messages to the plugin.</param>
    public static IServiceCollection AddPluginMessenger(this IServiceCollection services, string pluginName)
    {
        services.TryAddSingleton<IMessageTransport, InProcessTransport>();
        services.TryAddSingleton<IPluginMessenger>(serviceProvider => new PluginMessenger(
            pluginName,
            serviceProvider.GetRequiredService<IMessageTransport>(),
            serviceProvider.GetRequiredService<IErrorReporter>(),
            serviceProvider.GetRequiredService<ILogger<PluginMessenger>>()));

        return services;
    }
}
