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
    const string RestrictedAreasItem = "restrictedAreasToolStripMenuItem";
    const string MapsItem = "mapsToolStripMenuItem";
    const string InfoItem = "aISToolStripMenuItem";
    const string FindItem = "findToolStripMenuItem";
    const string AirWindowItem = "secondASDToolStripMenuItem";
    const string GroundWindowItem = "ASMGCSToolStripMenuItem";
    const string FlightPlanItem = "flightPlanToolStripMenuItem";
    const string StripSetupItem = "setupToolStripMenuItem2";
    const string MessagesItem = "messagesToolStripMenuItem";
    const string SendMessageItem = "sendToolStripMenuItem";
    const string ToolsItem = "toolsToolStripMenuItem";
    const string WindowsItem = "windowsToolStripMenuItem";
    const string AtisItem = "aTISToolStripMenuItem";
    const string AtisRequestItem = "requestToolStripMenuItem";

    // In the vatSys ATIS menu, the online ATIS stations come after this separator
    const string AtisStationsSeparator = "toolStripSeparator4";
    const string RemoveQuickTagItem = "removeToolStripMenuItem";
    const string ConflictAreaItem = "latcToolStripMenuItem";
    const string SelcalItem = "sELCALToolStripMenuItem";

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
    const string AdsContractWindowTitle = "ADS Contract";
    const string CpdlcHistoryWindowTitle = "CPDLC History";

    readonly VatSysMenu _vatSysMenu = null!;
    readonly VatSysInternals _vatSysInternals = null!;
    readonly IErrorReporter _errorReporter = null!;

    // True after this class starts to listen to the vatSys Messages item
    bool _isWatchingMessagesItem;

    // The open windows that this class listens to, so that a button is turned off as soon as its window closes
    readonly HashSet<Form> _watchedWindows = [];

    [ObservableProperty] string stripSortMode = string.Empty;
    [ObservableProperty] bool isHoldSetupAvailable;
    [ObservableProperty] bool isTfmsAvailable;
    [ObservableProperty] bool isAdsContractAvailable;
    [ObservableProperty] bool isCpdlcHistoryAvailable;
    [ObservableProperty] bool isHoldSetupOpen;
    [ObservableProperty] bool isStripSetupOpen;
    [ObservableProperty] bool isAdsContractOpen;
    [ObservableProperty] bool isCpdlcHistoryOpen;
    [ObservableProperty] bool hasUnreadMessages;

    // Example data for the XAML designer. Do not use at runtime.
    public BottomMenuViewModel()
    {
        StripSortMode = "A";
        IsHoldSetupAvailable = true;
        IsTfmsAvailable = true;
        IsAdsContractAvailable = true;
        IsCpdlcHistoryAvailable = true;
    }

    public BottomMenuViewModel(VatSysMenu vatSysMenu, VatSysInternals vatSysInternals, IErrorReporter errorReporter)
    {
        _vatSysMenu = vatSysMenu;
        _vatSysInternals = vatSysInternals;
        _errorReporter = errorReporter;
    }

    /// <summary>
    /// Reads the vatSys state again.
    /// </summary>
    public void Refresh()
    {
        RefreshUnreadMessages();

        // The first letter of Alpha, Time, or Level
        StripSortMode = MMI.StripSortMode.ToString().Substring(0, 1);

        IsHoldSetupAvailable = _vatSysMenu.FindPluginItem(ToolsItem, HoldSetupText) is not null;
        IsTfmsAvailable = _vatSysMenu.FindPluginCategory(TfmsCategory) is not null;
        IsAdsContractAvailable = FindCpdlcItem(AdsContractText) is not null;
        IsCpdlcHistoryAvailable = FindCpdlcItem(CpdlcHistoryText) is not null;

        RefreshWindows();
    }

    public IReadOnlyList<PopupMenuItem> GetSetUpMenuItems()
    {
        return GetMenuItems(() =>
        [
            _vatSysMenu.GetPopupMenuItem(_vatSysMenu.GetItem(DisplaySettingsItem), "Display"),
            _vatSysMenu.GetPopupMenuItem(_vatSysMenu.GetItem(AudioSettingsItem), "Audio"),
            _vatSysMenu.GetPopupMenuItem(_vatSysMenu.GetItem(KeyboardSettingsItem), "Keyboard"),
            _vatSysMenu.GetPopupMenuItem(_vatSysMenu.GetItem(RestrictedAreasItem), "Restricted Areas")
        ]);
    }

    public IReadOnlyList<PopupMenuItem> GetMapMenuItems()
    {
        return GetMenuItems(() => _vatSysMenu.GetPopupMenuItems(_vatSysMenu.GetItem(MapsItem)));
    }

    public IReadOnlyList<PopupMenuItem> GetInfoMenuItems()
    {
        return GetMenuItems(() =>
        {
            // In vatSys, Find has a submenu with a text field. Here, Find opens the text box at once.
            var findItem = _vatSysMenu.GetTextInputItem(_vatSysMenu.GetItem(FindItem), "Find...", title: null);

            return _vatSysMenu.GetPopupMenuItems(
                _vatSysMenu.GetItem(InfoItem),
                new Dictionary<string, PopupMenuItem> { [FindItem] = findItem });
        });
    }

    public IReadOnlyList<PopupMenuItem> GetTfmsMenuItems()
    {
        return GetMenuItems(() => _vatSysMenu.FindPluginCategory(TfmsCategory) is { } tfms
            ? _vatSysMenu.GetPopupMenuItems(tfms)
            : []);
    }

    /// <summary>
    /// Gets the items of the ATIS menu:
    /// <list type="bullet">
    /// <item>Edit opens the ATIS Editor.</item>
    /// <item>
    /// View has Request, which opens an ATIS window for an airport, and the online ATIS stations, which are the same as
    /// in the vatSys ATIS menu.
    /// </item>
    /// </list>
    /// </summary>
    public IReadOnlyList<PopupMenuItem> GetAtisMenuItems()
    {
        return GetMenuItems(() =>
        {
            var atisMenu = _vatSysMenu.GetItem(AtisItem);
            var atisEditorItem = _vatSysMenu.FindPluginItem(WindowsItem, AtisEditorText);

            // Request puts the ICAO code in the vatSys Request text field. Thus, vatSys opens the ATIS window, the
            // same as from its own menu.
            var requestItem = _vatSysMenu.GetTextInputItem(
                _vatSysMenu.GetItem(AtisRequestItem),
                "Request...",
                "Request ATIS");

            var stationItems = atisMenu.DropDownItems
                .Cast<ToolStripItem>()
                .SkipWhile(item => item.Name != AtisStationsSeparator)
                .Skip(1)
                .OfType<ToolStripMenuItem>()
                .Where(item => item.Available)
                .Select(item => _vatSysMenu.GetPopupMenuItem(item));

            return
            [
                new PopupMenuItem(
                    "Edit",
                    () => ClickPluginItem(atisEditorItem),
                    isEnabled: atisEditorItem is not null),
                new PopupMenuItem("View", children: [requestItem, ..stationItems])
            ];
        });
    }

    /// <summary>
    /// Marks the new messages as seen, so that the MSG button stops flashing. The same as a click on Messages in the
    /// vatSys menu bar. The popup menu still shows the number of unread messages of each item.
    /// </summary>
    public void ClearUnreadMessages()
    {
        Run(() =>
        {
            _vatSysMenu.GetItem(MessagesItem).Checked = false;
            RefreshUnreadMessages();
        });
    }

    public IReadOnlyList<PopupMenuItem> GetMessagesMenuItems()
    {
        return GetMenuItems(() =>
        {
            var messagesMenu = _vatSysMenu.GetItem(MessagesItem);

            // In vatSys, Send has a submenu with a text field for the callsign. Here, Send opens the text box at once.
            var replacements = new Dictionary<string, PopupMenuItem>
            {
                [SendMessageItem] = _vatSysMenu.GetTextInputItem(
                    _vatSysMenu.GetItem(SendMessageItem),
                    "Send...",
                    title: null)
            };

            // vatSys opens a private message window when its item is released, and not when it is clicked. Thus, these
            // items open the window with the public vatSys method.
            foreach (var recipient in _vatSysInternals.GetPrivateMessageRecipients())
            {
                if (messagesMenu.DropDownItems[recipient] is not ToolStripMenuItem privateMessageItem)
                    continue;

                replacements[recipient] = new PopupMenuItem(
                    privateMessageItem.Text,
                    () => Run(() => OpenPrivateMessage(privateMessageItem)),
                    unreadCount: privateMessageItem.Tag is int count ? count : 0);
            }

            return _vatSysMenu.GetPopupMenuItems(messagesMenu, replacements);
        });
    }

    // Shows the private message window, and marks its messages as read, the same as its item in the vatSys Messages
    // menu
    static void OpenPrivateMessage(ToolStripMenuItem privateMessageItem)
    {
        MMI.OpenPMWindow(privateMessageItem.Name);
        privateMessageItem.CheckState = CheckState.Unchecked;
        privateMessageItem.Tag = null;
    }

    [RelayCommand]
    void OpenAirWindow() => Click(AirWindowItem);

    [RelayCommand]
    void OpenGroundWindow() => Click(GroundWindowItem);

    [RelayCommand]
    void OpenFlightPlan() => Click(FlightPlanItem);

    /// <summary>
    /// Opens the vatSys Quick Tag window for the selected track in the middle of the screen.
    /// </summary>
    [RelayCommand]
    void AddQuickTag() => Run(_vatSysInternals.OpenQuickTagWindow);

    /// <summary>
    /// Removes the quick tag of the selected track, the same as Tools, Quick Tag, Remove.
    /// </summary>
    [RelayCommand]
    void RemoveQuickTag() => Click(RemoveQuickTagItem);

    /// <summary>
    /// Starts or stops the lateral conflict (LATC) tool for the selected track, the same as Tools, Conflict Area, and
    /// the LATC key (F2 by default).
    /// </summary>
    [RelayCommand]
    void ToggleLateralConflictTool() => Click(ConflictAreaItem);

    /// <summary>
    /// Opens the vatSys SELCAL window for the selected aircraft, the same as Tools, SELCAL. vatSys does this only when
    /// it is connected as a controller.
    /// </summary>
    [RelayCommand]
    void OpenSelcal() => Click(SelcalItem);

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
                OnPropertyChanged(nameof(IsAdsContractOpen));
                OnPropertyChanged(nameof(IsCpdlcHistoryOpen));
            }
        });
    }

    // vatSys checks its Messages item when a message is received in the System chat, the Controller chat, or a private
    // message. Then the vatSys menu bar flashes the item.
    void RefreshUnreadMessages()
    {
        var messagesItem = _vatSysMenu.GetItem(MessagesItem);

        // The refresh is only at an interval. Thus, listen to the item, so that the button flashes at once.
        if (!_isWatchingMessagesItem)
        {
            _isWatchingMessagesItem = true;
            messagesItem.CheckedChanged += (_, _) => Run(RefreshUnreadMessages);
        }

        HasUnreadMessages = messagesItem.Checked;
    }

    /// <param name="closingWindow">
    /// A window that is closing. It can still be visible, but the button must show that it is closed.
    /// </param>
    void RefreshWindows(Form? closingWindow = null)
    {
        IsHoldSetupOpen = FindHoldSetupWindow(closingWindow) is not null;
        IsStripSetupOpen = FindStripSetupWindow(closingWindow) is not null;
        IsAdsContractOpen = FindAdsContractWindow(closingWindow) is not null;
        IsCpdlcHistoryOpen = FindCpdlcHistoryWindow(closingWindow) is not null;
    }

    Form? FindHoldSetupWindow(Form? closingWindow = null) =>
        FindWindow(window => window.Text == HoldSetupWindowTitle, closingWindow);

    Form? FindStripSetupWindow(Form? closingWindow = null) =>
        FindWindow(window => window.GetType().Name == StripSetupWindowTypeName, closingWindow);

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
