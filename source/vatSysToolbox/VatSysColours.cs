using System.Windows.Media;
using vatsys;
using vatSysToolbox.Controls;

namespace vatSysToolbox;

/// <summary>
/// The colours of vatSys.
/// To use a vatSys colour in XAML, use <see cref="VatSysColourExtension"/>.
/// To use a vatSys colour in code, use <see cref="GetColour"/>.
/// </summary>
/// <seealso cref="VatSysFont"/>
/// <seealso cref="Bevel"/>
public static class VatSysColours
{
    static readonly Dictionary<Colours.Identities, SolidColorBrush> ColourCache = new();

    /// <summary>
    /// Gets a vatSys colour from the profile. If the profile does not have the colour, vatSys gives its default colour.
    /// The brush is frozen, so you can use it from all threads.
    /// </summary>
    public static SolidColorBrush GetColour(Colours.Identities identity)
    {
        lock (ColourCache)
        {
            if (ColourCache.TryGetValue(identity, out var cachedBrush))
                return cachedBrush;

            var colour = Colours.GetColour(identity);
            var brush = new SolidColorBrush(Color.FromArgb(colour.A, colour.R, colour.G, colour.B));
            brush.Freeze();

            // Before vatSys loads the colours, it gives a fallback colour. Do not keep the fallback colour.
            if (Colours.Loaded)
                ColourCache[identity] = brush;

            return brush;
        }
    }
}
