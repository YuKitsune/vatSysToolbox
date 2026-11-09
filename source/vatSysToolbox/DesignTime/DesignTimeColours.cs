using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace vatSysToolbox.DesignTime;

/// <summary>
/// The default vatSys colours, for the XAML designer.
/// The colours come from DefaultColours.xml, which is a copy of the default vatSys Colours.xml file.
/// </summary>
/// <remarks>
/// The designer cannot load the vatSys types. Thus, this class uses the colour names, and not
/// <c>Colours.Identities</c>.
/// </remarks>
static class DesignTimeColours
{
    const string ResourceName = "vatSysToolbox.DesignTime.DefaultColours.xml";

    // The Custom colours do not have an identity
    const string CustomColourName = "Custom";

    static readonly Lazy<Dictionary<string, SolidColorBrush>> DefaultColours = new(LoadDefaultColours);

    /// <summary>
    /// Gets the default colour. If DefaultColours.xml does not have the colour, gives the same fallback colour as vatSys.
    /// The brush is frozen.
    /// </summary>
    public static SolidColorBrush GetColour(string name)
    {
        return DefaultColours.Value.TryGetValue(name, out var brush)
            ? brush
            : CreateBrush(SystemColors.ControlDarkColor);
    }

    static Dictionary<string, SolidColorBrush> LoadDefaultColours()
    {
        using var stream = typeof(DesignTimeColours).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The resource {ResourceName} does not exist.");

        var colours = new Dictionary<string, SolidColorBrush>();
        foreach (var element in XDocument.Load(stream).Root!.Elements("Colour"))
        {
            var name = (string?)element.Attribute("id");
            if (string.IsNullOrEmpty(name) || name == CustomColourName)
                continue;

            colours[name!] = CreateBrush(Color.FromRgb(
                (byte)(int)element.Element("R")!,
                (byte)(int)element.Element("G")!,
                (byte)(int)element.Element("B")!));
        }

        return colours;
    }

    static SolidColorBrush CreateBrush(Color colour)
    {
        var brush = new SolidColorBrush(colour);
        brush.Freeze();
        return brush;
    }
}
