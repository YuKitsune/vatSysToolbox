using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using vatsys;

namespace EurocatMenusPlugin.ViewModels;

/// <summary>
/// A strip in the Tag List for one quick tag.
/// </summary>
public partial class TagListItemViewModel : ObservableObject
{
    [ObservableProperty] string displayAircraftId = string.Empty;
    [ObservableProperty] string displayCodeAndLevel = string.Empty;
    [ObservableProperty] string globalOps = string.Empty;
    [ObservableProperty] bool isEditingGlobalOps;
    [ObservableProperty] string globalOpsEditValue = string.Empty;

    // Example data for the XAML designer. Do not use at runtime.
    public TagListItemViewModel(string aircraftId, string codeAndLevel, string globalOps)
    {
        DisplayAircraftId = aircraftId.PadRight(8);
        DisplayCodeAndLevel = codeAndLevel;
        GlobalOps = globalOps;
    }

    public TagListItemViewModel(QuickTag quickTag)
    {
        QuickTag = quickTag;
        Refresh();
    }

    public QuickTag QuickTag { get; } = null!;

    /// <summary>
    /// Reads the quick tag again.
    /// </summary>
    public void Refresh()
    {
        DisplayAircraftId = QuickTag.Callsign.PadRight(8);
        DisplayCodeAndLevel = $" A{FormatCode(QuickTag.AssignedSSRCode)} {FormatLevel(QuickTag.CoupledTrack.CorrectedAltitude)} ";
        GlobalOps = QuickTag.LabelData;
    }

    // vatSys keeps the SSR code as a number, and shows it in octal
    static string FormatCode(int code) => Convert.ToString(code, 8).PadLeft(4, '0');

    // The altitude in hundreds of feet. vatSys sets the altitude to -1 when it is not known.
    static string FormatLevel(int altitude) => altitude < 0 ? "   " : (altitude / 100).ToString("000");

    [RelayCommand]
    void EditGlobalOps()
    {
        GlobalOpsEditValue = GlobalOps;
        IsEditingGlobalOps = true;
    }

    [RelayCommand]
    void CommitGlobalOpsEdit()
    {
        if (!IsEditingGlobalOps)
            return;

        // vatSys shows this text in the label of the quick tag
        QuickTag.LabelData = GlobalOpsEditValue;
        GlobalOps = GlobalOpsEditValue;
        IsEditingGlobalOps = false;
    }

    [RelayCommand]
    void CancelGlobalOpsEdit()
    {
        IsEditingGlobalOps = false;
        GlobalOpsEditValue = string.Empty;
    }
}
