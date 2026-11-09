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

    // The windows that the toggle buttons open. A toggle button is on while its window is open.
    const string StripSetupWindowTypeName = "StripSetupWindow";
    const string HoldSetupWindowTitle = "Hold Setup";
    const string AtisEditorWindowTitle = "ATIS Editor";
    const string AdsContractWindowTitle = "ADS Contract";
    const string CpdlcHistoryWindowTitle = "CPDLC History";

    readonly VatSysMenu _vatSysMenu = null!;
    readonly IErrorReporter _errorReporter = null!;

    // The open windows that this class listens to, so that a button is turned off as soon as its window closes
    readonly HashSet<Form> _watchedWindows = [];

    [ObservableProperty] string stripSortMode = string.Empty;
    [ObservableProperty] bool isHoldSetupAvailable;
    [ObservableProperty] bool isTfmsAvailable;
    [ObservableProperty] bool isAdsContractAvailable;
    [ObservableProperty] bool isCpdlcHistoryAvailable;
    [ObservableProperty] bool isAtisEditorAvailable;
    [ObservableProperty] bool isHoldSetupOpen;
    [ObservableProperty] bool isStripSetupOpen;
    [ObservableProperty] bool isAtisEditorOpen;
    [ObservableProperty] bool isAdsContractOpen;
    [ObservableProperty] bool isCpdlcHistoryOpen;

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

        RefreshWindows();
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
    void ToggleStripSetup() => ToggleWindow(
        () => FindStripSetupWindow(),
        () => _vatSysMenu.Click(StripSetupItem),
        isOpen => IsStripSetupOpen = isOpen);

    [RelayCommand]
    void ToggleHoldSetup() => ToggleWindow(
        () => FindHoldSetupWindow(),
        () => ClickPluginItem(_vatSysMenu.FindPluginItem(ToolsItem, HoldSetupText)),
        isOpen => IsHoldSetupOpen = isOpen);

    [RelayCommand]
    void ToggleAdsContract() => ToggleWindow(
        () => FindAdsContractWindow(),
        () => ClickPluginItem(FindCpdlcItem(AdsContractText)),
        isOpen => IsAdsContractOpen = isOpen);

    [RelayCommand]
    void ToggleCpdlcHistory() => ToggleWindow(
        () => FindCpdlcHistoryWindow(),
        () => ClickPluginItem(FindCpdlcItem(CpdlcHistoryText)),
        isOpen => IsCpdlcHistoryOpen = isOpen);

    [RelayCommand]
    void ToggleAtisEditor() => ToggleWindow(
        () => FindAtisEditorWindow(),
        () => ClickPluginItem(_vatSysMenu.FindPluginItem(WindowsItem, AtisEditorText)),
        isOpen => IsAtisEditorOpen = isOpen);

    // Closes the window if it is open. Otherwise, opens it.
    void ToggleWindow(Func<Form?> findWindow, Action open, Action<bool> setIsOpen)
    {
        Run(() =>
        {
            var isOpening = false;
            try
            {
                if (findWindow() is { } window)
                {
                    window.Close();
                }
                else
                {
                    open();
                    isOpening = true;
                }
            }
            finally
            {
                RefreshWindows();

                // Some plugins open their window later (for example, the CPDLC plugin uses a work queue). Thus, keep
                // the button pressed. If the window does not open, the next refresh releases the button.
                if (isOpening)
                    setIsOpen(true);

                // A toggle button changes its own state when it is clicked. If the window did not open or close, the
                // properties do not send a change, and the button shows the wrong state. Thus, send the states again.
                OnPropertyChanged(nameof(IsHoldSetupOpen));
                OnPropertyChanged(nameof(IsStripSetupOpen));
                OnPropertyChanged(nameof(IsAtisEditorOpen));
                OnPropertyChanged(nameof(IsAdsContractOpen));
                OnPropertyChanged(nameof(IsCpdlcHistoryOpen));
            }
        });
    }

    /// <param name="closingWindow">
    /// A window that is closing. It can still be visible, but the button must show that it is closed.
    /// </param>
    void RefreshWindows(Form? closingWindow = null)
    {
        IsHoldSetupOpen = FindHoldSetupWindow(closingWindow) is not null;
        IsStripSetupOpen = FindStripSetupWindow(closingWindow) is not null;
        IsAtisEditorOpen = FindAtisEditorWindow(closingWindow) is not null;
        IsAdsContractOpen = FindAdsContractWindow(closingWindow) is not null;
        IsCpdlcHistoryOpen = FindCpdlcHistoryWindow(closingWindow) is not null;
    }

    Form? FindHoldSetupWindow(Form? closingWindow = null) =>
        FindWindow(window => window.Text == HoldSetupWindowTitle, closingWindow);

    Form? FindStripSetupWindow(Form? closingWindow = null) =>
        FindWindow(window => window.GetType().Name == StripSetupWindowTypeName, closingWindow);

    Form? FindAtisEditorWindow(Form? closingWindow = null) =>
        FindWindow(window => window.Text == AtisEditorWindowTitle, closingWindow);

    Form? FindAdsContractWindow(Form? closingWindow = null) =>
        FindWindow(window => window.Text == AdsContractWindowTitle, closingWindow);

    Form? FindCpdlcHistoryWindow(Form? closingWindow = null) =>
        FindWindow(window => window.Text == CpdlcHistoryWindowTitle, closingWindow);

    Form? FindWindow(Func<Form, bool> isWindow, Form? closingWindow)
    {
        var window = Application.OpenForms
            .Cast<Form>()
            .FirstOrDefault(form => form != closingWindow && form.Visible && isWindow(form));

        // The refresh is only at an interval. Thus, listen to the window, so that its button is turned off as soon as
        // the window closes.
        if (window is not null && _watchedWindows.Add(window))
        {
            window.VisibleChanged += WatchedWindow_VisibleChanged;
            window.FormClosed += WatchedWindow_FormClosed;
        }

        return window;
    }

    // vatSys hides some windows, and does not close them
    void WatchedWindow_VisibleChanged(object sender, EventArgs e) => Run(() => RefreshWindows());

    void WatchedWindow_FormClosed(object sender, FormClosedEventArgs e)
    {
        var window = (Form)sender;
        window.VisibleChanged -= WatchedWindow_VisibleChanged;
        window.FormClosed -= WatchedWindow_FormClosed;
        _watchedWindows.Remove(window);

        Run(() => RefreshWindows(window));
    }

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
