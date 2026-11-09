using System.IO;
using System.Reflection;
using vatsys;

namespace vatSysToolbox.Configuration;

public static class ProfileDirectoryResolver
{
    /// <summary>
    /// Gets the directory of the vatSys profile that is loaded.
    /// </summary>
    /// <returns><c>false</c> if no profile is loaded.</returns>
    public static bool TryGetProfileDirectory(out DirectoryInfo? directoryInfo)
    {
        directoryInfo = null;
        if (!Profile.Loaded)
            return false;

        // vatSys does not make the short name of the profile public
        var shortNameObject = typeof(Profile).GetField("shortName", BindingFlags.Static | BindingFlags.NonPublic);
        var shortName = (string)shortNameObject.GetValue(shortNameObject);

        directoryInfo = new DirectoryInfo(Path.Combine(Helpers.GetFilesFolder(), "Profiles", shortName));
        return true;
    }
}
