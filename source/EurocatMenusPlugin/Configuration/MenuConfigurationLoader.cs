using System.IO;
using vatSysToolbox.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EurocatMenusPlugin.Configuration;

/// <summary>
/// Reads the configuration file. The file is in the profile, or in the directory of the plugin. The plugin comes with
/// a default file.
/// </summary>
public static class MenuConfigurationLoader
{
    public const string FileName = "EurocatMenus.yaml";

    // The value of the type key of each bottom menu item
    static readonly Dictionary<string, Type> BottomMenuItemTypes = new()
    {
        ["menu"] = typeof(MenuItemConfiguration),
        ["indicator"] = typeof(IndicatorItemConfiguration),
        ["stack"] = typeof(StackItemConfiguration),
        ["separator"] = typeof(SeparatorItemConfiguration),
        ["button"] = typeof(ButtonItemConfiguration),
        ["toggle"] = typeof(ToggleItemConfiguration)
    };

    /// <exception cref="FileNotFoundException">The file is not in the profile or in the directory of the plugin.</exception>
    public static MenuConfiguration Load()
    {
        var filePath = ConfigurationLocator.LocateConfigurationFile(typeof(Plugin).Assembly, FileName, Plugin.Name);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)

            // The payload of a message has no type. Thus, read true as a boolean and 1 as a number, and not as text,
            // so that the JSON of the payload has the same types.
            .WithAttemptingUnquotedStringTypeDeserialization()
            .WithTypeDiscriminatingNodeDeserializer(options => options.AddKeyValueTypeDiscriminator<BottomMenuItemConfiguration>(
                "type",
                BottomMenuItemTypes))

            // The items of a menu are a built-in menu or a list
            .WithNodeDeserializer(new MenuItemsNodeDeserializer(), syntax => syntax.OnTop())
            .Build();

        using var reader = new StreamReader(filePath);
        return deserializer.Deserialize<MenuConfiguration?>(reader) ?? new MenuConfiguration();
    }
}
