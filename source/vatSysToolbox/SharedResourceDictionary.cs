using System.Windows;
using vatSysToolbox.DesignTime;

namespace vatSysToolbox;

/// <summary>
/// A resource dictionary that loads each source one time, and shares it between all the views that use it.
/// </summary>
/// <remarks>
/// <para>
/// WPF parses a merged dictionary again for each view that merges it. <c>Styles.xaml</c> is large, and a view that
/// opens often (for example, a window or a popup) pays that cost each time it opens. This dictionary keeps the parsed
/// dictionary of each source, and merges the same instance into each view.
/// </para>
/// <para>
/// In the XAML designer, the dictionary loads the source the normal way, so that the designer shows the changes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;UserControl.Resources&gt;
///     &lt;ResourceDictionary&gt;
///         &lt;ResourceDictionary.MergedDictionaries&gt;
///             &lt;toolbox:SharedResourceDictionary Source="/vatSysToolbox;component/Styles.xaml" /&gt;
///         &lt;/ResourceDictionary.MergedDictionaries&gt;
///     &lt;/ResourceDictionary&gt;
/// &lt;/UserControl.Resources&gt;
/// </code>
/// </example>
public class SharedResourceDictionary : ResourceDictionary
{
    static readonly Dictionary<Uri, ResourceDictionary> Cache = new();

    Uri? _source;

    /// <summary>
    /// The location of the dictionary. The dictionary is loaded the first time that a view uses the location, and
    /// then shared.
    /// </summary>
    public new Uri? Source
    {
        get => _source;
        set
        {
            _source = value;
            if (value is null)
                return;

            if (DesignMode.IsActive)
            {
                base.Source = value;
                return;
            }

            MergedDictionaries.Add(GetOrLoad(value));
        }
    }

    static ResourceDictionary GetOrLoad(Uri source)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(source, out var dictionary))
            {
                dictionary = new ResourceDictionary { Source = source };
                Cache[source] = dictionary;
            }

            return dictionary;
        }
    }
}
