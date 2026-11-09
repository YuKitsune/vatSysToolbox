using System.Runtime.CompilerServices;
using System.Windows.Markup;
using System.Windows.Media;
using vatsys;
using vatSysToolbox.DesignTime;

namespace vatSysToolbox;

/// <summary>
/// Gives a vatSys colour from the profile as a brush.
/// In the XAML designer, gives the default vatSys colour.
/// </summary>
/// <remarks>
/// The colour is a name, and not a <see cref="Colours.Identities"/> value. In the XAML designer, vatSys is not running,
/// and the designer cannot load the vatSys types. Thus, this class must not use the vatSys types in design mode.
/// If the name is not a vatSys colour, the view fails when it loads in vatSys.
/// </remarks>
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

    public VatSysColourExtension(string identity)
    {
        Identity = identity;
    }

    /// <summary>
    /// The name of a <see cref="Colours.Identities"/> value (for example, <c>InteractiveText</c>).
    /// </summary>
    [ConstructorArgument("identity")]
    public string Identity { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return DesignMode.IsActive
            ? DesignTimeColours.GetColour(Identity)
            : GetVatSysColour(Identity);
    }

    // The JIT compiler loads the vatSys types when it compiles the method that uses them.
    // This method is not inlined. Thus, the vatSys types load only when this method runs, and not in design mode.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static SolidColorBrush GetVatSysColour(string name)
    {
        if (!Enum.TryParse<Colours.Identities>(name, out var identity) ||
            !Enum.IsDefined(typeof(Colours.Identities), identity))
            throw new ArgumentException($"{name} is not a vatSys colour.", nameof(name));

        return VatSysColours.GetColour(identity);
    }
}
