using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using vatsys;
using vatSysToolbox;

namespace EurocatMenusPlugin.ViewModels;

public partial class BottomMenuViewModel : ObservableObject
{
    // The names of the vatSys menu items
    const string DisplaySettingsItem = "displayToolStripMenuItem1";
    const string AudioSettingsItem = "audioToolStripMenuItem";
    const string KeyboardSettingsItem = "keyToolStripMenuItem";
    const string MapsItem = "mapsToolStripMenuItem";
    const string InfoItem = "aISToolStripMenuItem";
    const string AirWindowItem = "secondASDToolStripMenuItem";
    const string GroundWindowItem = "ASMGCSToolStripMenuItem";
    const string FlightPlanItem = "flightPlanToolStripMenuItem";
    const string StripSetupItem = "setupToolStripMenuItem2";
    const string MessagesItem = "messagesToolStripMenuItem";
    const string ToolsItem = "toolsToolStripMenuItem";
    const string WindowsItem = "windowsToolStripMenuItem";
    const string AtisItem = "aTISToolStripMenuItem";

    // vatSys adds an item to the ATIS menu for each online ATIS station. The name of the item is the callsign of the
    // station, which has this suffix. The text of the item is the ICAO code.
    const string AtisCallsignSuffix = "_ATIS";

    // The menu items of other plugins
    // TODO: Use the messaging system to open the windows of other plugins, and not their menu items.
    const string HoldSetupText = "Hold Setup";
    const string CpdlcCategory = "CPDLC";
    const string AdsContractText = "ADS Contract";
    const string CpdlcHistoryText = "History";
    const string TfmsCategory = "TFMS";
    const string AtisEditorText = "ATIS Editor";

    readonly VatSysMenu _vatSysMenu = null!;
    readonly IErrorReporter _errorReporter = null!;

    [ObservableProperty] string stripSortMode = string.Empty;
    [ObservableProperty] bool isHoldSetupAvailable;
    [ObservableProperty] bool isTfmsAvailable;
    [ObservableProperty] bool isAdsContractAvailable;
    [ObservableProperty] bool isCpdlcHistoryAvailable;
    [ObservableProperty] bool isAtisEditorAvailable;

    // Example data for the XAML designer. Do not use at runtime.
    public BottomMenuViewModel()
    {
        StripSortMode = "A";
        IsHoldSetupAvailable = true;
        IsTfmsAvailable = true;
        IsAdsContractAvailable = true;
        IsCpdlcHistoryAvailable = true;
        IsAtisEditorAvailable = true;
    }

    public BottomMenuViewModel(VatSysMenu vatSysMenu, IErrorReporter errorReporter)
    {
        _vatSysMenu = vatSysMenu;
        _errorReporter = errorReporter;
    }

    /// <summary>
    /// Reads the vatSys state again.
    /// </summary>
    public void Refresh()
    {
        // The first letter of Alpha, Time, or Level
        StripSortMode = MMI.StripSortMode.ToString().Substring(0, 1);

        IsHoldSetupAvailable = _vatSysMenu.FindPluginItem(ToolsItem, HoldSetupText) is not null;
        IsTfmsAvailable = _vatSysMenu.FindPluginCategory(TfmsCategory) is not null;
        IsAdsContractAvailable = FindCpdlcItem(AdsContractText) is not null;
        IsCpdlcHistoryAvailable = FindCpdlcItem(CpdlcHistoryText) is not null;
        IsAtisEditorAvailable = _vatSysMenu.FindPluginItem(WindowsItem, AtisEditorText) is not null;
    }

    public IReadOnlyList<PopupMenuItem> GetSetUpMenuItems()
    {
        return GetMenuItems(() =>
        [
            _vatSysMenu.GetPopupMenuItem(_vatSysMenu.GetItem(DisplaySettingsItem), "Display"),
            _vatSysMenu.GetPopupMenuItem(_vatSysMenu.GetItem(AudioSettingsItem), "Audio"),
            _vatSysMenu.GetPopupMenuItem(_vatSysMenu.GetItem(KeyboardSettingsItem), "Keyboard")
        ]);
    }

    public IReadOnlyList<PopupMenuItem> GetMapMenuItems()
    {
        return GetMenuItems(() => _vatSysMenu.GetPopupMenuItems(_vatSysMenu.GetItem(MapsItem)));
    }

    public IReadOnlyList<PopupMenuItem> GetInfoMenuItems()
    {
        return GetMenuItems(() => _vatSysMenu.GetPopupMenuItems(_vatSysMenu.GetItem(InfoItem)));
    }

    public IReadOnlyList<PopupMenuItem> GetTfmsMenuItems()
    {
        return GetMenuItems(() => _vatSysMenu.FindPluginCategory(TfmsCategory) is { } tfms
            ? _vatSysMenu.GetPopupMenuItems(tfms)
            : []);
    }

    /// <summary>
    /// Gets an item for each online ATIS station. A click on an item opens the ATIS window for the station.
    /// </summary>
    public IReadOnlyList<PopupMenuItem> GetAtisMenuItems()
    {
        return GetMenuItems(() => _vatSysMenu.GetItem(AtisItem).DropDownItems
            .OfType<ToolStripMenuItem>()
            .Where(item => item.Name.EndsWith(AtisCallsignSuffix))
            .Select(item => _vatSysMenu.GetPopupMenuItem(item))
            .ToList());
    }

    public IReadOnlyList<PopupMenuItem> GetMessagesMenuItems()
    {
        return GetMenuItems(() => _vatSysMenu.GetPopupMenuItems(_vatSysMenu.GetItem(MessagesItem)));
    }

    [RelayCommand]
    void OpenAirWindow() => Click(AirWindowItem);

    [RelayCommand]
    void OpenGroundWindow() => Click(GroundWindowItem);

    [RelayCommand]
    void OpenFlightPlan() => Click(FlightPlanItem);

    [RelayCommand]
    void OpenStripSetup() => Click(StripSetupItem);

    [RelayCommand]
    void OpenHoldSetup() => ClickPluginItem(_vatSysMenu.FindPluginItem(ToolsItem, HoldSetupText));

    [RelayCommand]
    void OpenAdsContract() => ClickPluginItem(FindCpdlcItem(AdsContractText));

    [RelayCommand]
    void OpenCpdlcHistory() => ClickPluginItem(FindCpdlcItem(CpdlcHistoryText));

    [RelayCommand]
    void OpenAtisEditor() => ClickPluginItem(_vatSysMenu.FindPluginItem(WindowsItem, AtisEditorText));

    ToolStripMenuItem? FindCpdlcItem(string text)
    {
        return _vatSysMenu.FindPluginCategory(CpdlcCategory)?.DropDownItems
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(item => item.Text == text);
    }

    void Click(string name)
    {
        try
        {
            _vatSysMenu.Click(name);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    void ClickPluginItem(ToolStripMenuItem? item)
    {
        try
        {
            if (item is null)
                throw new InvalidOperationException("The plugin is not loaded");

            item.PerformClick();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    IReadOnlyList<PopupMenuItem> GetMenuItems(Func<IReadOnlyList<PopupMenuItem>> getItems)
    {
        try
        {
            return getItems();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
            return [];
        }
    }
}
