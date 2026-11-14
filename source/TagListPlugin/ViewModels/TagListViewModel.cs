using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using vatsys;
using vatSysToolbox;
using vatSysToolbox.Windows;

namespace TagListPlugin.ViewModels;

/// <summary>
/// The list of quick tags. The items are in the same order as in vatSys. The list gets the changes from the vatSys
/// events that <see cref="VatSysPatches"/> sends.
/// </summary>
/// <remarks>
/// Make this class on the UI thread. The messenger keeps only a weak reference to it.
/// </remarks>
public partial class TagListViewModel : ObservableObject
{
    readonly IGuiInvoker _guiInvoker = null!;
    readonly IErrorReporter _errorReporter = null!;

    public ObservableCollection<TagListItemViewModel> Items { get; } = [];

    // Example data for the XAML designer. Do not use at runtime.
    public TagListViewModel()
    {
        Items.Add(new TagListItemViewModel("QFA1", " A1234 015 ", "TEST"));
        Items.Add(new TagListItemViewModel("VH-ABC", " A4321 120 ", string.Empty));
    }

    public TagListViewModel(IMessenger messenger, IGuiInvoker guiInvoker, IErrorReporter errorReporter)
    {
        _guiInvoker = guiInvoker;
        _errorReporter = errorReporter;

        // The list gets the events for all the items. If each item subscribed when it is added, the events that come
        // just after the add event (for example, the global ops text after an edit) could come before the
        // subscription.
        messenger.Register<TagListViewModel, QuickTagAddedMessage>(
            this,
            (viewModel, message) => viewModel.OnGui(() => viewModel.AddItem(message.QuickTag)));
        messenger.Register<TagListViewModel, QuickTagRemovedMessage>(
            this,
            (viewModel, message) => viewModel.OnGui(() => viewModel.RemoveItem(message.QuickTag)));
        messenger.Register<TagListViewModel, QuickTagLabelDataChangedMessage>(
            this,
            (viewModel, message) => viewModel.OnGui(() =>
                viewModel.FindItem(message.QuickTag)?.SetGlobalOps(message.QuickTag.LabelData)));
        // vatSys sends this event for all the radar tracks at each radar update. Most tracks have no quick tag. Do not
        // post these to the UI thread.
        messenger.Register<TagListViewModel, RadarTrackAltitudeChangedMessage>(
            this,
            (viewModel, message) =>
            {
                if (message.RadarTrack.QuickTag is not null)
                    viewModel.OnGui(() => viewModel.SetAltitude(message.RadarTrack));
            });

        foreach (var quickTag in RDP.QuickTags)
        {
            AddItem(quickTag);
        }
    }

    // vatSys sends the events on its own thread
    void OnGui(Action action) => _guiInvoker.InvokeOnGUI(_ => action());

    void AddItem(QuickTag quickTag)
    {
        if (FindItem(quickTag) is not null)
            return;
        
        Items.Add(new TagListItemViewModel(quickTag));
    }

    void RemoveItem(QuickTag quickTag)
    {
        if (FindItem(quickTag) is not { } item)
            return;
        
        Items.Remove(item);
    }

    // vatSys can couple a quick tag to a new radar track (for example, when the aircraft reconnects with the same
    // code). Thus, find the item from the quick tag of the radar track.
    void SetAltitude(RDP.RadarTrack radarTrack)
    {
        // vatSys can remove the quick tag before this runs on the UI thread
        if (radarTrack.QuickTag is null)
            return;

        FindItem(radarTrack.QuickTag)?.SetAltitude(radarTrack.CorrectedAltitude);
    }

    TagListItemViewModel? FindItem(QuickTag quickTag)
    {
        return Items.FirstOrDefault(item => item.QuickTag == quickTag);
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
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Cannot delete the quick tag");
        }
    }
}
