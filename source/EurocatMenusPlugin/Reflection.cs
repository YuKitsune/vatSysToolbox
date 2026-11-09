using System.Reflection;
using System.Runtime.ExceptionServices;

namespace EurocatMenusPlugin;

/// <summary>
/// Gets to members that vatSys does not make public. A vatSys update can change these members. Then these methods
/// throw an exception that gives the name of the member.
/// </summary>
static class Reflection
{
    const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static object? GetField(object target, string name)
    {
        var field = target.GetType().GetField(name, InstanceFlags)
            ?? throw new MissingMemberException(target.GetType().Name, name);
        return field.GetValue(target);
    }

    public static object? GetStaticProperty(Type type, string name)
    {
        var property = type.GetProperty(name, StaticFlags)
            ?? throw new MissingMemberException(type.Name, name);
        return property.GetValue(null);
    }

    public static object? InvokeMethod(object target, string name, params object?[] arguments)
    {
        var method = target.GetType().GetMethod(name, InstanceFlags)
            ?? throw new MissingMemberException(target.GetType().Name, name);

        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Show the exception from vatSys, and not the exception from reflection
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
