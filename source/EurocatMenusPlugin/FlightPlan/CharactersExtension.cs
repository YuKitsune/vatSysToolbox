using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using vatSysToolbox;

namespace EurocatMenusPlugin.FlightPlan;

/// <summary>
/// Gives the width of a field that holds a number of characters of the vatSys font. The width includes the bevel and
/// the padding of the field.
/// </summary>
/// <example>
/// <code>
/// &lt;TextBox Width="{local:Characters 4}" /&gt;
/// </code>
/// </example>
[MarkupExtensionReturnType(typeof(double))]
public class CharactersExtension : MarkupExtension
{
    // The bevel (2) and the padding (2) on each side, and 2 pixels so that the last character is not against the bevel
    const double Frame = 2 * (2 + 2) + 2;

    static readonly Lazy<double> CharacterWidth = new(MeasureCharacter);

    public CharactersExtension()
    {
    }

    /// <param name="count">The number of characters.</param>
    public CharactersExtension(int count)
    {
        Count = count;
    }

    /// <summary>
    /// The number of characters.
    /// </summary>
    [ConstructorArgument("count")]
    public int Count { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Count * CharacterWidth.Value + Frame;
    }

    // The vatSys font is monospaced. If it is not, the widest character gives a field that holds all the characters.
    static double MeasureCharacter()
    {
        var typeface = new Typeface(VatSysFont.Family, FontStyles.Normal, VatSysFont.Weight, FontStretches.Normal);
        var text = new FormattedText(
            "W",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            VatSysFont.Size,
            Brushes.Black,
            pixelsPerDip: 1);

        return Math.Ceiling(text.WidthIncludingTrailingWhitespace);
    }
}
