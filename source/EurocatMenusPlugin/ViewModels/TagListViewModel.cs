using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using vatsys;
using vatSysToolbox;

namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// The list of quick tags. The items are in the same order as in vatSys.
/// </summary>
public partial class TagListViewModel : ObservableObject
{
    readonly IErrorReporter _errorReporter = null!;

    public ObservableCollection<TagListItemViewModel> Items { get; } = [];

    // Example data for the XAML designer. Do not use at runtime.
    public TagListViewModel()
    {
        Items.Add(new TagListItemViewModel("QFA1", " A1234 015 ", "TEST"));
        Items.Add(new TagListItemViewModel("VH-ABC", " A4321 120 ", string.Empty));
    }

    public TagListViewModel(IErrorReporter errorReporter)
    {
        _errorReporter = errorReporter;
    }

    /// <summary>
    /// Shows the quick tags again.
    /// </summary>
    public void Refresh(IList<QuickTag> quickTags)
    {
        var deletedItems = Items.Where(item => !quickTags.Contains(item.QuickTag)).ToList();
        foreach (var deletedItem in deletedItems)
            Items.Remove(deletedItem);

        for (var i = 0; i < quickTags.Count; i++)
        {
            var item = Items.FirstOrDefault(item => item.QuickTag == quickTags[i]);
            if (item is null)
            {
                Items.Insert(i, new TagListItemViewModel(quickTags[i]));
                continue;
            }

            item.Refresh();

            var currentIndex = Items.IndexOf(item);
            if (currentIndex != i)
                Items.Move(currentIndex, i);
        }
    }

    [RelayCommand]
    void RemoveQuickTag(TagListItemViewModel item)
    {
        try
        {
            RDP.RemoveQuickTag(item.QuickTag);

            // vatSys does not change the state of the track when the quick tag is removed. Thus, do the same as vatSys
            // and set the state again, so that the track is not shown as jurisdiction.
            if (MMI.FindTrack(item.QuickTag.CoupledTrack) is { } track)
                MMI.SetTrackState(track);

            Items.Remove(item);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Cannot delete the quick tag");
        }
    }
}
