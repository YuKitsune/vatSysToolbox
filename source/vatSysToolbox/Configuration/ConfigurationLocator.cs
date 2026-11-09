using System.IO;
using System.Reflection;

namespace vatSysToolbox.Configuration;

public static class ConfigurationLocator
{
    /// <summary>
    /// Finds a configuration file. This method searches these directories in this sequence:
    /// <list type="number">
    ///     <item>Profile\Plugins\Configs\{<paramref name="plugin"/>.Name} (for each name in <paramref name="configDirectoryNames"/>)</item>
    ///     <item>Profile\Plugins\Configs</item>
    ///     <item>Profile\Plugins</item>
    ///     <item>Profile</item>
    ///     <item>The directory of <paramref name="plugin"/></item> assembly.
    /// </list>
    /// </summary>
    /// <exception cref="FileNotFoundException">The file is not in one of the directories.</exception>
    public static string LocateConfigurationFile(vatsys.IPlugin plugin, string fileName)
    {
        return LocateConfigurationFile(plugin.GetType().Assembly, fileName, [plugin.Name]);
    }
    
    /// <summary>
    /// Finds a configuration file. This method searches these directories in this sequence:
    /// <list type="number">
    ///     <item>Profile\Plugins\Configs\{configDirectoryName} (for each name in <paramref name="configDirectoryNames"/>)</item>
    ///     <item>Profile\Plugins\Configs</item>
    ///     <item>Profile\Plugins</item>
    ///     <item>Profile</item>
    ///     <item>The directory of <paramref name="pluginAssembly"/></item>
    /// </list>
    /// </summary>
    /// <exception cref="FileNotFoundException">The file is not in one of the directories.</exception>
    public static string LocateConfigurationFile(Assembly pluginAssembly, string fileName, params string[] configDirectoryNames)
    {
        var searchDirectories = new List<string>();

        // Search the profile first
        if (ProfileDirectoryResolver.TryGetProfileDirectory(out var profileDirectory) && profileDirectory is not null)
        {
            foreach (var configDirectoryName in configDirectoryNames)
            {
                searchDirectories.Add(Path.Combine(profileDirectory.FullName, "Plugins", "Configs", configDirectoryName));
            }

            searchDirectories.AddRange([
                Path.Combine(profileDirectory.FullName, "Plugins", "Configs"),
                Path.Combine(profileDirectory.FullName, "Plugins"),
                profileDirectory.FullName
            ]);
        }

        // Search the assembly directory last
        var assemblyDirectory = Path.GetDirectoryName(pluginAssembly.Location);
        searchDirectories.Add(assemblyDirectory);

        foreach (var searchDirectory in searchDirectories)
        {
            var filePath = Path.Combine(searchDirectory, fileName);
            if (!File.Exists(filePath))
                continue;

            return filePath;
        }

        throw new FileNotFoundException($"Unable to locate {fileName}");
    }
}
