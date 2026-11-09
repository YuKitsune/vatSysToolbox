using System.Windows.Markup;
using System.Windows.Media;
using vatsys;

namespace vatSysToolbox;

/// <summary>
/// Gives a vatSys colour from the profile as a brush.
/// </summary>
/// <example>
/// <code>
/// &lt;TextBlock Foreground="{local:VatSysColour InteractiveText}" /&gt;
/// </code>
/// </example>
[MarkupExtensionReturnType(typeof(SolidColorBrush))]
public class VatSysColourExtension : MarkupExtension
{
    public VatSysColourExtension()
    {
    }

    public VatSysColourExtension(Colours.Identities identity)
    {
        Identity = identity;
    }

    [ConstructorArgument("identity")]
    public Colours.Identities Identity { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return VatSysColours.GetColour(Identity);
    }
}
