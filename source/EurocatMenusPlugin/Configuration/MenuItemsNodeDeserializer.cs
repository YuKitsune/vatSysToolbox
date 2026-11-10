using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace EurocatMenusPlugin.Configuration;

/// <summary>
/// Reads <see cref="MenuItemsConfiguration"/>. A text (for example, <c>builtin/map-menu</c>) gives a
/// <see cref="BuiltInMenuItemsConfiguration"/>, and a list gives a <see cref="CustomMenuItemsConfiguration"/>.
/// </summary>
public class MenuItemsNodeDeserializer : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer)
    {
        value = null;
        if (expectedType != typeof(MenuItemsConfiguration))
            return false;

        if (reader.TryConsume<Scalar>(out var scalar))
        {
            if (!scalar.Value.StartsWith(BuiltInMenuItemsConfiguration.Prefix))
                throw new YamlException(
                    scalar.Start,
                    scalar.End,
                    $"The items {scalar.Value} must be a list, or start with {BuiltInMenuItemsConfiguration.Prefix}");

            value = new BuiltInMenuItemsConfiguration(
                scalar.Value.Substring(BuiltInMenuItemsConfiguration.Prefix.Length));
            return true;
        }

        var items = (List<BottomMenuItemConfiguration>?)nestedObjectDeserializer(
            reader,
            typeof(List<BottomMenuItemConfiguration>));
        value = new CustomMenuItemsConfiguration(items ?? []);
        return true;
    }
}
