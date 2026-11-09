using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using vatsys;

namespace vatSysToolbox.DesignTime;

/// <summary>
/// The default vatSys colours, for the XAML designer.
/// The colours come from DefaultColours.xml, which is a copy of the default vatSys Colours.xml file.
/// </summary>
static class DesignTimeColours
{
    const string ResourceName = "vatSysToolbox.DesignTime.DefaultColours.xml";

    static readonly Lazy<Dictionary<Colours.Identities, Color>> DefaultColours = new(LoadDefaultColours);

    /// <summary>
    /// Gets the default colour. If DefaultColours.xml does not have the colour, gives the same fallback colour as vatSys.
    /// </summary>
    public static Color GetColour(Colours.Identities identity)
    {
        return DefaultColours.Value.TryGetValue(identity, out var colour)
            ? colour
            : SystemColors.ControlDarkColor;
    }

    static Dictionary<Colours.Identities, Color> LoadDefaultColours()
    {
        using var stream = typeof(DesignTimeColours).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The resource {ResourceName} does not exist.");

        var colours = new Dictionary<Colours.Identities, Color>();
        foreach (var element in XDocument.Load(stream).Root!.Elements("Colour"))
        {
            // Ignore the Custom colours, because they do not have an identity
            if (!Enum.TryParse<Colours.Identities>((string?)element.Attribute("id"), out var identity) ||
                identity == Colours.Identities.Custom)
                continue;

            colours[identity] = Color.FromRgb(
                (byte)(int)element.Element("R")!,
                (byte)(int)element.Element("G")!,
                (byte)(int)element.Element("B")!);
        }

        return colours;
    }
}
