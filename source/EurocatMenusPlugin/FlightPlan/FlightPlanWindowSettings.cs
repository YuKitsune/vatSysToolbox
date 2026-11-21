using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using vatsys;
using vatSysToolbox;

namespace EurocatMenusPlugin.FlightPlan;

/// <summary>
/// Keeps the selection of the Flight Plan Window (the window of this plugin, or the vatSys window) in a file, so that
/// vatSys starts with the same window the next time.
/// </summary>
/// <remarks>
/// The file is in the vatSys files folder, and not in the profile. Thus, the selection is the same for all profiles,
/// and a profile update does not remove it.
/// </remarks>
/// <param name="filePath">The full path of the file.</param>
/// <param name="errorReporter">Reports the errors that occur when the file is read or written.</param>
public class FlightPlanWindowSettings(string filePath, IErrorReporter errorReporter)
{
    const string DirectoryName = "EurocatMenus";
    const string FileName = "Settings.json";

    // The key in the file
    const string IsEurocatWindowEnabledKey = "eurocatFlightPlanWindow";

    static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>
    /// The path of the file in the vatSys files folder.
    /// </summary>
    public static string DefaultFilePath => Path.Combine(Helpers.GetFilesFolder(), DirectoryName, FileName);

    /// <summary>
    /// Reads the selection. If there is no file, or the file cannot be read, the window of this plugin is selected.
    /// </summary>
    /// <returns><c>true</c> if the window of this plugin is selected.</returns>
    public bool Load()
    {
        try
        {
            return ReadFile()?[IsEurocatWindowEnabledKey]?.GetValue<bool>() ?? true;
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, $"Cannot read {filePath}");
            return true;
        }
    }

    /// <summary>
    /// Writes the selection. The other keys in the file do not change.
    /// </summary>
    /// <param name="isEurocatWindowEnabled"><c>true</c> if the window of this plugin is selected.</param>
    public void Save(bool isEurocatWindowEnabled)
    {
        try
        {
            var settings = TryReadFile() ?? new JsonObject();
            settings[IsEurocatWindowEnabledKey] = isEurocatWindowEnabled;

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, settings.ToJsonString(WriteOptions));
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, $"Cannot write {filePath}");
        }
    }

    JsonObject? ReadFile()
    {
        if (!File.Exists(filePath))
            return null;

        return JsonNode.Parse(File.ReadAllText(filePath)) as JsonObject;
    }

    // If the file is not correct JSON, write a new file
    JsonObject? TryReadFile()
    {
        try
        {
            return ReadFile();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
