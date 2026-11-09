using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using vatsys;
using vatSysToolbox;

namespace EurocatMenusPlugin.ViewModels;

public partial class TopMenuViewModel : ObservableObject
{
    // The names of the vatSys menu items
    const string ConnectItem = "connectToolStripMenuItem";
    const string PositionDisplayItem = "positionToolStripDisplay";
    const string PositionsItem = "positionsToolStripMenuItem";
    const string VisibilityCentersItem = "visibilityCentersToolStripMenuItem";
    const string AutomaticVisibilityCentersItem = "automaticToolStripMenuItem";
    const string SectorsItem = "sectorsToolStripMenuItem";
    const string VelocityVectorsItem = "velocityVectorsToolStripMenuItem";
    const string VelocityVectorsOffItem = "offToolStripMenuItem";
    const string ShortRouteProbeDistanceItem = "srpdistanceToolStripMenuItem";
    const string ShortRouteProbeTimeItem = "srptimeToolStripMenuItem";
    const string ShortRouteProbeDefaultOnItem = "srpoffToolStripMenuItem";
    const string HistoryDotsItem = "historyTrailToolStripMenuItem";
    const string HistoryDotsOffItem = "offToolStripMenuItem2";
    const string SsrAllItem = "showSSRToolStripMenuItem";
    const string QuickLookAllItem = "showFilteredToolStripMenuItem";
    const string FpasdItem = "fPASDToolStripMenuItem";
    const string ExtendedLabelsItem = "extendedToolStripMenuItem";
    const string SharedLabelsItem = "sharedLabelsToolStripMenuItem";

    // The status of a display when the Air window is not open
    const string DefaultDisplayStatus = "DEF";

    const string DisconnectedText = "OFFLINE";

    // The Sectors button shows this text when no sectors are selected
    const string NoSectorsText = "SECTORS";

    // The Sectors button shows this after the first sector when more than one sector group is selected
    const string MoreSectorsSuffix = "*";

    // The level filters are in hundreds of feet, and the text boxes show three digits
    const int LevelFilterUnit = 100;
    const int LevelFilterLength = 3;

    // SSR code filter 1 uses the first two digits of the codes. SSR code filter 2 uses all four digits.
    const int SsrCodeFilter1Length = 2;
    const int SsrCodeFilter2Length = 4;

    readonly VatSysMenu _vatSysMenu = null!;
    readonly VatSysInternals _vatSysInternals = null!;
    readonly IClock _clock = null!;
    readonly IErrorReporter _errorReporter = null!;

    // The SSR code filters that the text boxes show. The text boxes can have a value that is not yet applied
    // (for example, a start code without an end code). Thus, they get new values only when the filter changes.
    SSRCodeFilter? _shownSsrCodeFilter1;
    SSRCodeFilter? _shownSsrCodeFilter2;

    [ObservableProperty] string connection = string.Empty;
    [ObservableProperty] string positionName = string.Empty;
    [ObservableProperty] string visibilityCentersMode = string.Empty;
    [ObservableProperty] string sectors = string.Empty;

    [ObservableProperty] string mainWindowStatus = string.Empty;
    [ObservableProperty] string secondWindowStatus = string.Empty;
    [ObservableProperty] bool isSecondWindowOpen;

    [ObservableProperty] string time = string.Empty;
    [ObservableProperty] string timer = string.Empty;

    [ObservableProperty] bool isVelocityVectorsOn;
    [ObservableProperty] string velocityVectorLength = string.Empty;
    [ObservableProperty] bool isShortRouteProbeOn;
    [ObservableProperty] string shortRouteProbeLength = string.Empty;
    [ObservableProperty] bool isHistoryDotsOn;
    [ObservableProperty] string historyDotCount = string.Empty;
    [ObservableProperty] bool isSsrAllOn;
    [ObservableProperty] bool isQuickLookAllOn;
    [ObservableProperty] bool isFpasdOn;
    [ObservableProperty] string labelType = string.Empty;
    [ObservableProperty] string labelSharing = string.Empty;

    // These properties are the values of text boxes. A text box sets its property only when Enter is pressed.
    // The setter applies the value in vatSys, and then reads the value from vatSys again.
    string _mainWindowRange = string.Empty;
    string _secondWindowRange = string.Empty;
    string _lowerDisplayLevel = string.Empty;
    string _upperDisplayLevel = string.Empty;
    string _ssrCodeFilter1Start = string.Empty;
    string _ssrCodeFilter1End = string.Empty;
    string _ssrCodeFilter2Start = string.Empty;
    string _ssrCodeFilter2End = string.Empty;

    // Example data for the XAML designer. Do not use at runtime.
    public TopMenuViewModel()
    {
        Connection = "ML-BIK_CTR";
        PositionName = "APPROACH";
        VisibilityCentersMode = "AUTO";
        Sectors = "BIK*";
        _mainWindowRange = "165";
        MainWindowStatus = "RST";
        _secondWindowRange = "300";
        SecondWindowStatus = "DEF";
        IsSecondWindowOpen = true;
        _lowerDisplayLevel = "000";
        _upperDisplayLevel = "999";
        Time = "00:36:57";
        Timer = "0024";
        IsVelocityVectorsOn = true;
        VelocityVectorLength = "1";
        ShortRouteProbeLength = "5 T";
        IsHistoryDotsOn = true;
        HistoryDotCount = "6";
        IsFpasdOn = true;
        LabelType = "Normal";
        LabelSharing = "Fixed";
    }

    public TopMenuViewModel(
        VatSysMenu vatSysMenu,
        VatSysInternals vatSysInternals,
        IClock clock,
        IErrorReporter errorReporter)
    {
        _vatSysMenu = vatSysMenu;
        _vatSysInternals = vatSysInternals;
        _clock = clock;
        _errorReporter = errorReporter;
    }

    public string MainWindowRange
    {
        get => _mainWindowRange;
        set
        {
            Run(() =>
            {
                if (int.TryParse(value, out var range))
                    _vatSysInternals.MainAsd.SetRange(range);

                RefreshMainWindow();
            });
        }
    }

    public string SecondWindowRange
    {
        get => _secondWindowRange;
        set
        {
            Run(() =>
            {
                if (int.TryParse(value, out var range))
                    _vatSysInternals.FindSecondAsd()?.SetRange(range);

                RefreshSecondWindow();
            });
        }
    }

    public string LowerDisplayLevel
    {
        get => _lowerDisplayLevel;
        set
        {
            Run(() =>
            {
                try
                {
                    if (value.Length == LevelFilterLength && int.TryParse(value, out var level))
                        MMI.LowerLevelFilter = level;
                }
                finally
                {
                    RefreshDisplayLevels();
                }
            });
        }
    }

    public string UpperDisplayLevel
    {
        get => _upperDisplayLevel;
        set
        {
            Run(() =>
            {
                try
                {
                    if (value.Length == LevelFilterLength && int.TryParse(value, out var level))
                        MMI.UpperLevelFilter = level;
                }
                finally
                {
                    RefreshDisplayLevels();
                }
            });
        }
    }

    public string SsrCodeFilter1Start
    {
        get => _ssrCodeFilter1Start;
        set
        {
            SetProperty(ref _ssrCodeFilter1Start, value);
            Run(ApplySsrCodeFilter1);
        }
    }

    public string SsrCodeFilter1End
    {
        get => _ssrCodeFilter1End;
        set
        {
            SetProperty(ref _ssrCodeFilter1End, value);
            Run(ApplySsrCodeFilter1);
        }
    }

    public string SsrCodeFilter2Start
    {
        get => _ssrCodeFilter2Start;
        set
        {
            SetProperty(ref _ssrCodeFilter2Start, value);
            Run(ApplySsrCodeFilter2);
        }
    }

    public string SsrCodeFilter2End
    {
        get => _ssrCodeFilter2End;
        set
        {
            SetProperty(ref _ssrCodeFilter2End, value);
            Run(ApplySsrCodeFilter2);
        }
    }

    /// <summary>
    /// Reads the vatSys state again.
    /// </summary>
    public void Refresh()
    {
        Connection = Network.IsConnected ? Network.Callsign : DisconnectedText;
        PositionName = _vatSysMenu.GetItem(PositionDisplayItem).Text;
        VisibilityCentersMode = _vatSysMenu.GetItem(AutomaticVisibilityCentersItem).Checked ? "AUTO" : "MAN";
        Sectors = FormatSectors(MMI.SectorsControlled);

        RefreshMainWindow();
        RefreshSecondWindow();
        RefreshDisplayLevels();
        RefreshSsrCodeFilters();
        RefreshTime();
        RefreshFunctionalStates();
    }

    public IReadOnlyList<PopupMenuItem> GetPositionMenuItems()
    {
        return GetMenuItems(() => _vatSysMenu.GetPopupMenuItems(_vatSysMenu.GetItem(PositionsItem)));
    }

    public IReadOnlyList<PopupMenuItem> GetVisibilityCentersMenuItems()
    {
        return GetMenuItems(() => _vatSysMenu.GetPopupMenuItems(_vatSysMenu.GetItem(VisibilityCentersItem)));
    }

    public IReadOnlyList<PopupMenuItem> GetVelocityVectorLengthMenuItems()
    {
        return GetMenuItems(() => GetLengthMenuItems(VelocityVectorsItem, VelocityVectorsOffItem));
    }

    public IReadOnlyList<PopupMenuItem> GetShortRouteProbeLengthMenuItems()
    {
        return GetMenuItems(() =>
        [
            _vatSysMenu.GetPopupMenuItem(_vatSysMenu.GetItem(ShortRouteProbeDistanceItem)),
            _vatSysMenu.GetPopupMenuItem(_vatSysMenu.GetItem(ShortRouteProbeTimeItem))
        ]);
    }

    public IReadOnlyList<PopupMenuItem> GetHistoryDotCountMenuItems()
    {
        return GetMenuItems(() => GetLengthMenuItems(HistoryDotsItem, HistoryDotsOffItem));
    }

    [RelayCommand]
    void OpenConnection() => Click(ConnectItem);

    [RelayCommand]
    void OpenSectors() => Click(SectorsItem);

    [RelayCommand]
    void OpenTimeMenu()
    {
        Run(() =>
        {
            _vatSysInternals.OpenTimeMenu();
            RefreshTime();
        });
    }

    [RelayCommand]
    void ToggleVelocityVectors()
    {
        ToggleFunctionalState(() => ToggleLength(
            VelocityVectorsItem,
            VelocityVectorsOffItem,
            MMI.VelocityVectors > 0,
            _vatSysInternals.StoredVelocityVectorLength));
    }

    [RelayCommand]
    void ToggleShortRouteProbe()
    {
        ToggleFunctionalState(() =>
        {
            var selectedTrack = GetSelectedTrackWithFlightPlan();
            if (selectedTrack is null)
            {
                // Turn the short route probes on or off for all aircraft
                _vatSysMenu.Click(ShortRouteProbeDefaultOnItem);
                return;
            }

            // Turn the short route probe on or off for the selected aircraft only
            var isVisible = !IsShortRouteProbeVisible(selectedTrack);

            // If the aircraft is now the same as all aircraft, it follows the setting for all aircraft again.
            // Otherwise, a later change to the setting for all aircraft does not change this aircraft.
            if (isVisible == MMI.SRPDefaultOn)
                selectedTrack.SRPVisible = MMI.SRPVisibilities.Global;
            else
                selectedTrack.SRPVisible = isVisible ? MMI.SRPVisibilities.On : MMI.SRPVisibilities.Off;
        });
    }

    [RelayCommand]
    void ToggleHistoryDots()
    {
        ToggleFunctionalState(() => ToggleLength(
            HistoryDotsItem,
            HistoryDotsOffItem,
            MMI.HistoryDots > 0,
            _vatSysInternals.StoredHistoryDotCount));
    }

    [RelayCommand]
    void ToggleSsrAll() => ToggleFunctionalState(() => _vatSysMenu.Click(SsrAllItem));

    [RelayCommand]
    void ToggleQuickLookAll() => ToggleFunctionalState(() => _vatSysMenu.Click(QuickLookAllItem));

    [RelayCommand]
    void ToggleFpasd() => ToggleFunctionalState(() => _vatSysMenu.Click(FpasdItem));

    [RelayCommand]
    void ToggleExtendedLabels() => ToggleFunctionalState(() => _vatSysMenu.Click(ExtendedLabelsItem));

    [RelayCommand]
    void ToggleSharedLabels() => ToggleFunctionalState(() => _vatSysMenu.Click(SharedLabelsItem));

    void RefreshMainWindow()
    {
        var mainAsd = _vatSysInternals.MainAsd;
        SetProperty(ref _mainWindowRange, mainAsd.Range.ToString(), nameof(MainWindowRange));
        MainWindowStatus = mainAsd.Status;
    }

    void RefreshSecondWindow()
    {
        var secondAsd = _vatSysInternals.FindSecondAsd();
        IsSecondWindowOpen = secondAsd is not null;
        SetProperty(ref _secondWindowRange, secondAsd?.Range.ToString() ?? string.Empty, nameof(SecondWindowRange));
        SecondWindowStatus = secondAsd?.Status ?? DefaultDisplayStatus;
    }

    void RefreshDisplayLevels()
    {
        SetProperty(ref _lowerDisplayLevel, FormatLevelFilter(MMI.LowerLevelFilter), nameof(LowerDisplayLevel));
        SetProperty(ref _upperDisplayLevel, FormatLevelFilter(MMI.UpperLevelFilter), nameof(UpperDisplayLevel));
    }

    void RefreshSsrCodeFilters()
    {
        if (MMI.SSRCodeFilter1 != _shownSsrCodeFilter1)
            ShowSsrCodeFilter1();

        if (MMI.SSRCodeFilter2 != _shownSsrCodeFilter2)
            ShowSsrCodeFilter2();
    }

    void RefreshTime()
    {
        var now = _clock.UtcNow().UtcDateTime;
        Time = now.ToString("HH:mm:ss");
        Timer = FormatTimer(_vatSysInternals.TimerTime, now);
    }

    void RefreshFunctionalStates()
    {
        IsVelocityVectorsOn = MMI.VelocityVectors > 0;
        VelocityVectorLength = (IsVelocityVectorsOn
            ? MMI.VelocityVectors
            : _vatSysInternals.StoredVelocityVectorLength).ToString();

        var selectedTrack = GetSelectedTrackWithFlightPlan();
        IsShortRouteProbeOn = selectedTrack is null ? MMI.SRPDefaultOn : IsShortRouteProbeVisible(selectedTrack);
        ShortRouteProbeLength = selectedTrack is not null && selectedTrack.SRPType != MMI.SRPTypes.Global
            ? FormatShortRouteProbe(selectedTrack.SRPType, selectedTrack.SRPValue)
            : FormatShortRouteProbe(MMI.SRPType, MMI.SRPValue);

        IsHistoryDotsOn = MMI.HistoryDots > 0;
        HistoryDotCount = (IsHistoryDotsOn
            ? MMI.HistoryDots
            : _vatSysInternals.StoredHistoryDotCount).ToString();

        IsSsrAllOn = MMI.SSRAll;
        IsQuickLookAllOn = MMI.QuicklookAll;
        IsFpasdOn = MMI.FPASD;
        LabelType = MMI.ExtendedLabels ? "Extended" : "Normal";
        LabelSharing = MMI.SharedLabels ? "Shared" : "Fixed";
    }

    // Applies the filter only when both codes are complete, or both are empty. Until then, the text boxes keep the
    // code that was entered.
    void ApplySsrCodeFilter1()
    {
        var isEmpty = _ssrCodeFilter1Start.Length == 0 && _ssrCodeFilter1End.Length == 0;
        var isComplete = _ssrCodeFilter1Start.Length == SsrCodeFilter1Length &&
                         _ssrCodeFilter1End.Length == SsrCodeFilter1Length;
        if (!isEmpty && !isComplete)
            return;

        try
        {
            MMI.SSRCodeFilter1 = isEmpty ? null : new SSRCodeFilter(_ssrCodeFilter1Start, _ssrCodeFilter1End);
        }
        finally
        {
            ShowSsrCodeFilter1();
        }
    }

    void ApplySsrCodeFilter2()
    {
        var isEmpty = _ssrCodeFilter2Start.Length == 0 && _ssrCodeFilter2End.Length == 0;
        var isComplete = _ssrCodeFilter2Start.Length == SsrCodeFilter2Length &&
                         _ssrCodeFilter2End.Length == SsrCodeFilter2Length;
        if (!isEmpty && !isComplete)
            return;

        try
        {
            MMI.SSRCodeFilter2 = isEmpty ? null : new SSRCodeFilter(_ssrCodeFilter2Start, _ssrCodeFilter2End);
        }
        finally
        {
            ShowSsrCodeFilter2();
        }
    }

    void ShowSsrCodeFilter1()
    {
        _shownSsrCodeFilter1 = MMI.SSRCodeFilter1;
        SetProperty(ref _ssrCodeFilter1Start, FormatSsrCode(_shownSsrCodeFilter1?.StartCode, SsrCodeFilter1Length), nameof(SsrCodeFilter1Start));
        SetProperty(ref _ssrCodeFilter1End, FormatSsrCode(_shownSsrCodeFilter1?.EndCode, SsrCodeFilter1Length), nameof(SsrCodeFilter1End));
    }

    void ShowSsrCodeFilter2()
    {
        _shownSsrCodeFilter2 = MMI.SSRCodeFilter2;
        SetProperty(ref _ssrCodeFilter2Start, FormatSsrCode(_shownSsrCodeFilter2?.StartCode, SsrCodeFilter2Length), nameof(SsrCodeFilter2Start));
        SetProperty(ref _ssrCodeFilter2End, FormatSsrCode(_shownSsrCodeFilter2?.EndCode, SsrCodeFilter2Length), nameof(SsrCodeFilter2End));
    }

    // vatSys turns a value on with the item that has the value as its text, and turns it off with the Off item
    void ToggleLength(string parentName, string offItemName, bool isOn, int storedValue)
    {
        if (isOn)
        {
            _vatSysMenu.Click(offItemName);
            return;
        }

        var valueItem = _vatSysMenu.GetItem(parentName).DropDownItems
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(item => item.Text == storedValue.ToString())
            ?? throw new InvalidOperationException($"Cannot find the vatSys menu item for {storedValue}");
        valueItem.PerformClick();
    }

    // The values of a functional state, without the Off item
    IReadOnlyList<PopupMenuItem> GetLengthMenuItems(string parentName, string offItemName)
    {
        return _vatSysMenu.GetItem(parentName).DropDownItems
            .OfType<ToolStripMenuItem>()
            .Where(item => item.Name != offItemName)
            .Select(item => _vatSysMenu.GetPopupMenuItem(item))
            .ToList();
    }

    void ToggleFunctionalState(Action toggle)
    {
        Run(() =>
        {
            try
            {
                toggle();
            }
            finally
            {
                RefreshFunctionalStates();

                // A toggle button changes its own state when it is clicked. If the vatSys state did not change, the
                // properties do not send a change, and the button shows the wrong state. Thus, send the states again.
                OnPropertyChanged(nameof(IsVelocityVectorsOn));
                OnPropertyChanged(nameof(IsShortRouteProbeOn));
                OnPropertyChanged(nameof(IsHistoryDotsOn));
                OnPropertyChanged(nameof(IsSsrAllOn));
                OnPropertyChanged(nameof(IsQuickLookAllOn));
                OnPropertyChanged(nameof(IsFpasdOn));
            }
        });
    }

    void Click(string name) => Run(() => _vatSysMenu.Click(name));

    // vatSys uses the short route probe settings of the selected aircraft only if it has a flight plan
    static Track? GetSelectedTrackWithFlightPlan()
    {
        var selectedTrack = MMI.SelectedTrack;
        return selectedTrack?.GetFDR() is null ? null : selectedTrack;
    }

    static bool IsShortRouteProbeVisible(Track track)
    {
        return track.SRPVisible == MMI.SRPVisibilities.On ||
               (track.SRPVisible == MMI.SRPVisibilities.Global && MMI.SRPDefaultOn);
    }

    // Shows the ID of the first sector group, the same as the first row in the vatSys Sectors window. A sector group
    // is a selected sector that is not a sub-sector of another selected sector.
    static string FormatSectors(IList<SectorsVolumes.Sector> sectors)
    {
        var sectorGroups = sectors
            .Where(sector => !sectors.Any(group =>
                group != sector &&
                SectorsVolumes.SectorGroupings.TryGetValue(group, out var subSectors) &&
                subSectors.Contains(sector)))
            .ToList();

        if (sectorGroups.Count == 0)
            return NoSectorsText;

        return sectorGroups.Count == 1
            ? sectorGroups[0].Name
            : sectorGroups[0].Name + MoreSectorsSuffix;
    }

    static string FormatLevelFilter(int level) => (level / LevelFilterUnit).ToString("d3");

    // The same format as the vatSys filters menu
    static string FormatSsrCode(int? code, int length)
    {
        return code is null
            ? string.Empty
            : Convert.ToString(code.Value, 8).PadLeft(SsrCodeFilter2Length, '0').Substring(0, length);
    }

    static string FormatShortRouteProbe(MMI.SRPTypes type, int value)
    {
        return type == MMI.SRPTypes.Distance ? $"{value} D" : $"{value} T";
    }

    // The hours and minutes until the timer goes off. vatSys sets off the timer at the start of the minute.
    static string FormatTimer(DateTime timerTime, DateTime now)
    {
        if (timerTime == DateTime.MaxValue)
            return "0000";

        var alarmTime = timerTime.AddTicks(-(timerTime.Ticks % TimeSpan.TicksPerMinute));
        var remainingMinutes = Math.Max(0, (int)Math.Ceiling((alarmTime - now).TotalMinutes));
        return $"{remainingMinutes / 60:00}{remainingMinutes % 60:00}";
    }

    void Run(Action action)
    {
        // An exception that is not caught on the UI thread stops vatSys.
        try
        {
            action();
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
