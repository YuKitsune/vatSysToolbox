using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using vatsys;
using vatSysToolbox;
using States = vatsys.FDP2.FDR.FDRStates;

namespace EurocatMenusPlugin.FlightPlan;

/// <summary>
/// The state and the actions of <see cref="FlightPlanView"/>. It does the same vatSys actions as the vatSys Flight
/// Plan Window.
/// </summary>
/// <remarks>
/// <para>
/// Make the view model on the UI thread. The vatSys events can occur on any thread, and the view model moves them to
/// the UI thread.
/// </para>
/// <para>
/// The window disposes the view model when it closes. Then the view model stops listening to vatSys.
/// </para>
/// </remarks>
public partial class FlightPlanViewModel : ObservableObject, IDisposable
{
    enum Mode
    {
        View,
        Create,
        Modify,
        Depart
    }

    // The time to wait for the network to send a flight plan
    static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);

    // The FDR properties that change what the window shows
    static readonly HashSet<string> WatchedProperties =
    [
        nameof(FDP2.FDR.State), nameof(FDP2.FDR.ControllerTracking), nameof(FDP2.FDR.ControllingSector),
        nameof(FDP2.FDR.AircraftCount), nameof(FDP2.FDR.AircraftType), nameof(FDP2.FDR.AircraftWake),
        nameof(FDP2.FDR.AircraftEquip), nameof(FDP2.FDR.AircraftSurvEquip), nameof(FDP2.FDR.DepAirport),
        nameof(FDP2.FDR.DesAirport), nameof(FDP2.FDR.AltAirport), nameof(FDP2.FDR.ETD), nameof(FDP2.FDR.ATD),
        nameof(FDP2.FDR.EET), nameof(FDP2.FDR.TAS), nameof(FDP2.FDR.RFL), nameof(FDP2.FDR.Route),
        nameof(FDP2.FDR.ParsedRoute), nameof(FDP2.FDR.Remarks), nameof(FDP2.FDR.FlightRules),
        nameof(FDP2.FDR.AssignedSSRCode), nameof(FDP2.FDR.LabelOpData), nameof(FDP2.FDR.CoupledTrack),
        nameof(FDP2.FDR.InhibitAutoCouple), nameof(FDP2.FDR.Callsign)
    ];

    // The REG/ item in the remarks
    static readonly Regex RegistrationRegex = new(@"(?<![A-Z0-9])REG/(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    readonly IErrorReporter _errorReporter;
    readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

    // The fields that CRE and MOD edit
    readonly FieldViewModel[] _planFields;
    readonly FieldViewModel[] _allFieldsExceptAcid;

    FDP2.FDR? _fdr;
    Mode _mode = Mode.View;
    bool _requestPending;
    DispatcherTimer? _requestTimer;
    Action? _unsubscribeNoFlightPlan;
    Track? _routeTrack;
    bool _disposed;

    // ---- system row -----------------------------------------------------------

    public FieldViewModel Lpl { get; } = new();
    public FieldViewModel Csec { get; } = new();
    public FieldViewModel Pssr { get; } = new();
    public FieldViewModel Assr { get; } = new(maxLength: 4, allowedCharacters: "[0-7]");
    public FieldViewModel Opdata { get; } = new();
    public FieldViewModel Atd { get; } = new(maxLength: 4, allowedCharacters: "[0-9]");

    // ---- flight row -----------------------------------------------------------

    public FieldViewModel Acid { get; } = new(maxLength: 7);
    public FieldViewModel Frul { get; } = new(maxLength: 1);
    public FieldViewModel Typ1 { get; } = new(maxLength: 2, allowedCharacters: "[0-9]");
    public FieldViewModel Typ2 { get; } = new(maxLength: 4);
    public FieldViewModel Typ3 { get; } = new(maxLength: 1);
    public FieldViewModel Adep { get; } = new(maxLength: 4);
    public FieldViewModel Etd { get; } = new(maxLength: 4, allowedCharacters: "[0-9]");
    public FieldViewModel Ades { get; } = new(maxLength: 4);
    public FieldViewModel Rfl { get; } = new(maxLength: 3);

    // ---- route and remarks ----------------------------------------------------

    public FieldViewModel Rte { get; } = new();
    public FieldViewModel Other { get; } = new();

    // ---- equipment row --------------------------------------------------------

    public FieldViewModel Ftyp { get; } = new(maxLength: 1);
    public FieldViewModel Equip1 { get; } = new();
    public FieldViewModel Equip2 { get; } = new();
    public FieldViewModel Tas { get; } = new(maxLength: 4, allowedCharacters: "[0-9]");
    public FieldViewModel Eet { get; } = new(maxLength: 4, allowedCharacters: "[0-9]");
    public FieldViewModel Altn1 { get; } = new(maxLength: 4);
    public FieldViewModel Altn2 { get; } = new(maxLength: 4);
    public FieldViewModel Regn { get; } = new(maxLength: 7);

    // ---- labels ---------------------------------------------------------------

    [ObservableProperty] string counter = "0/0";
    [ObservableProperty] string flightPlanState = "";
    [ObservableProperty] string updateTime = "00:00:00";
    [ObservableProperty] string errorMessage = "";
    [ObservableProperty] bool hasError;

    // ---- mode buttons -----------------------------------------------------------

    [ObservableProperty] bool isCreateSelected;
    [ObservableProperty] bool isModifySelected;
    [ObservableProperty] bool isDepartSelected;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CreateCommand))] bool canCreate;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ModifyCommand))] bool canModify;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(DepartCommand))] bool canDepart;

    // ---- function buttons -------------------------------------------------------

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(EstimateCommand))] bool canEstimate;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BackCommand))] bool canBack;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CancelCommand))] bool canCancel;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(AcceptCommand))] bool canAccept;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(HandoffCommand))] bool canHandoff;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(InhibitCommand))] bool canInhibit;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(PrlCommand))] bool canPrl;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CflCommand))] bool canCfl;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(DecoupleCommand))] bool canDecouple;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CoupleCommand))] bool canCouple;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ToggleRouteCommand))] bool canToggleRoute;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(EnterCommand))] bool canEnter;

    /// <param name="fdr">The flight plan to show, or <c>null</c> for an empty window.</param>
    /// <param name="errorReporter">Reports the exceptions that occur in the window.</param>
    public FlightPlanViewModel(FDP2.FDR? fdr, IErrorReporter errorReporter)
    {
        _errorReporter = errorReporter;

        _planFields =
        [
            Frul, Typ1, Typ2, Typ3, Adep, Etd, Ades, Rfl, Rte, Other, Equip1, Equip2, Tas, Eet, Altn1, Regn
        ];

        _allFieldsExceptAcid =
        [
            Lpl, Csec, Pssr, Assr, Opdata, Atd, Frul, Typ1, Typ2, Typ3, Adep, Etd, Ades, Rfl, Rte, Other, Ftyp,
            Equip1, Equip2, Tas, Eet, Altn1, Altn2, Regn
        ];

        FDP2.FDRsChanged += OnFdrsChanged;
        Network.ValidATCChanged += OnValidAtcChanged;
        MMI.SelectedTrackChanged += OnSelectedTrackChanged;

        LoadFdr(fdr);
    }

    // =====================================================================
    //  Loading / state
    // =====================================================================

    /// <summary>
    /// Shows the flight plan, or clears the window if <paramref name="fdr"/> is <c>null</c>.
    /// </summary>
    void LoadFdr(FDP2.FDR? fdr)
    {
        Unsubscribe();
        HideGraphicRoute();
        CancelPendingRequest();

        _fdr = fdr;
        _mode = Mode.View;
        if (_fdr != null)
        {
            Acid.Set(_fdr.Callsign);
            _fdr.PropertyChanged += OnFdrPropertyChanged;
        }

        LoadFieldData(keepEdits: false);
        StampUpdateTime();
        RefreshState();
    }

    void Unsubscribe()
    {
        if (_fdr != null)
            _fdr.PropertyChanged -= OnFdrPropertyChanged;
    }

    /// <summary>
    /// Copies the FDR data into the fields. If <paramref name="keepEdits"/> is <c>true</c>, the fields that the user
    /// is typing in do not change.
    /// </summary>
    void LoadFieldData(bool keepEdits)
    {
        void Set(FieldViewModel field, string? value)
        {
            if (keepEdits && field.IsEditable && field.IsModified)
                return;

            field.Set(value);
        }

        var f = _fdr;
        if (f == null)
        {
            foreach (var field in _allFieldsExceptAcid)
                Set(field, "");

            return;
        }

        Set(Frul, f.FlightRules);
        Set(Typ1, f.AircraftCount.ToString());
        Set(Typ2, f.AircraftType);
        Set(Typ3, f.AircraftWake);
        Set(Equip1, f.AircraftEquip);
        Set(Equip2, f.AircraftSurvEquip);
        Set(Adep, f.DepAirport);
        Set(Etd, f.ETD.ToString("HHmm"));
        Set(Tas, f.TAS.ToString());
        Set(Rfl, (f.RFL / 100).ToString("D3"));
        Set(Ades, f.DesAirport);
        Set(Eet, f.EET.ToString("hhmm"));
        Set(Altn1, f.AltAirport);
        Set(Altn2, "");
        Set(Rte, f.Route);
        Set(Other, f.Remarks);
        Set(Regn, ParseRegistration(f.Remarks));
        Set(Ftyp, "");
        Set(Lpl, "");
        Set(Assr, f.AssignedSSRCode != -1 && SquawkAssignment.IsCodeDiscrete(f.AssignedSSRCode)
            ? Convert.ToString(f.AssignedSSRCode, 8).PadLeft(4, '0')
            : "");
        Set(Opdata, f.LabelOpData);
        Set(Atd, f.ATD != DateTime.MaxValue ? f.ATD.ToString("HHmm") : "");

        var sector = f.ControllingSector?.Name ?? "";
        Set(Csec, sector.Length > 3 ? sector.Substring(0, 3) + "*" : sector);

        var squawk = "";
        try
        {
            var pilot = f.CoupledTrack?.ActualAircraft;
            if (pilot != null)
                squawk = Convert.ToString(pilot.TransponderCode, 8).PadLeft(4, '0');
        }
        catch
        {
            // The coupled track can go while this reads it
        }

        Set(Pssr, squawk);
    }

    void StampUpdateTime()
    {
        UpdateTime = _fdr == null ? "00:00:00" : DateTime.UtcNow.ToString("HH:mm:ss");
    }

    static string StateText(States state)
    {
        return state switch
        {
            States.STATE_INACTIVE => "NACT",
            States.STATE_FINISHED => "FIN",
            States.STATE_HANDOVER or States.STATE_HANDOVER_FIRST => "HND",
            States.STATE_INHIBITED => "INHI",
            States.STATE_SUSPENDED => "SUS",
            States.STATE_UNCONTROLLED => "UCTL",
            States.STATE_PREACTIVE => "PREA",
            States.STATE_COORDINATED => "COOR",
            States.STATE_CONTROLLED => "CONT",
            _ => ""
        };
    }

    /// <summary>
    /// <c>true</c> if the label and system data (ASSR, OPDATA, PRL, CFL) can change. The same as the system button
    /// of the vatSys window.
    /// </summary>
    bool CanEditSystemData()
    {
        if (_fdr == null || !_fdr.HavePermission)
            return false;

        return _fdr.State switch
        {
            States.STATE_HANDOVER or States.STATE_HANDOVER_FIRST or States.STATE_INHIBITED or
                States.STATE_PREACTIVE or States.STATE_COORDINATED or States.STATE_CONTROLLED => true,
            _ => false
        };
    }

    /// <summary>
    /// The one place that derives the state of every button and field from the mode, the FDR, and the network.
    /// </summary>
    void RefreshState()
    {
        var fdr = _fdr;
        var has = fdr != null;
        var valid = Network.ValidATC;
        var perm = fdr is { HavePermission: true };
        var state = fdr?.State ?? States.STATE_NULL;
        var view = _mode == Mode.View && !_requestPending;

        var canCreate = valid;
        bool canMod = false, canCnl = false, canEst = false, canBack = false, canDep = false;
        bool canAcc = false, canHnd = false, canInhi = false;

        if (fdr != null)
        {
            var own = valid && (perm || !fdr.IsTracked);
            canCreate = own;
            canMod = own;
            canCnl = own;

            switch (state)
            {
                case States.STATE_INACTIVE:
                    canEst = true;
                    canCnl = false;
                    break;
                case States.STATE_FINISHED:
                    canMod = false;
                    canCnl = false;
                    break;
                case States.STATE_HANDOVER:
                case States.STATE_HANDOVER_FIRST:
                    canAcc = valid;
                    canCreate = false;
                    canInhi = perm;
                    break;
                case States.STATE_INHIBITED:
                    canInhi = true;
                    canAcc = valid;
                    canCreate = false;
                    break;
                case States.STATE_SUSPENDED:
                    canCreate = false;
                    break;
                case States.STATE_UNCONTROLLED:
                    canAcc = valid;
                    canCreate = false;
                    break;
                case States.STATE_PREACTIVE:
                    canEst = true;
                    canCreate = false;
                    break;
                case States.STATE_COORDINATED:
                    canDep = true;
                    canBack = fdr.ATD == DateTime.MaxValue;
                    canCreate = false;
                    canInhi = perm;
                    break;
                case States.STATE_CONTROLLED:
                    canDep = perm;
                    canHnd = perm;
                    canCreate = false;
                    canInhi = perm;
                    break;
            }
        }

        var sys = CanEditSystemData();

        // ---- mode buttons ----
        IsCreateSelected = _mode == Mode.Create;
        IsModifySelected = _mode == Mode.Modify;
        IsDepartSelected = _mode == Mode.Depart;

        CanCreate = _mode == Mode.Create || (view && canCreate);
        CanModify = _mode == Mode.Modify || (view && canMod);
        CanDepart = _mode == Mode.Depart || (view && canDep);

        // ---- function buttons ----
        CanEstimate = view && canEst;
        CanBack = view && canBack;
        CanCancel = view && canCnl;
        CanAccept = view && canAcc;
        CanHandoff = view && canHnd;
        CanInhibit = view && canInhi && valid;
        CanPrl = view && sys;
        CanCfl = view && sys;
        CanToggleRoute = has && _mode == Mode.View;
        CanEnter = !_requestPending;

        UpdateCoupleButtons(view);

        // ---- fields ----
        var editingPlan = _mode is Mode.Create or Mode.Modify;
        foreach (var field in _planFields)
            field.IsEditable = editingPlan;

        Acid.IsEditable = _mode == Mode.View;
        Atd.IsEditable = _mode == Mode.Depart;
        Assr.IsEditable = (view && sys) || _mode == Mode.Create;
        Opdata.IsEditable = (view && sys) || _mode == Mode.Create;

        // Display-only: vatSys has no data behind them, or the data is derived
        Lpl.IsEditable = false;
        Csec.IsEditable = false;
        Pssr.IsEditable = false;
        Ftyp.IsEditable = false;
        Altn2.IsEditable = false;

        FlightPlanState = has ? StateText(state) : "";
        Counter = has ? "1/1" : "0/0";
    }

    void UpdateCoupleButtons(bool view)
    {
        var canDecouple = false;
        var canCouple = false;

        if (_fdr != null && view)
        {
            if (_fdr.CoupledTrack != null)
            {
                canDecouple = true;
            }
            else
            {
                var selected = MMI.SelectedTrack;
                var selectedIsFreeRadarTrack = selected != null
                                               && selected.Type == Track.TrackTypes.TRACK_TYPE_RADAR
                                               && selected.GetFDR() == null;
                canCouple = selectedIsFreeRadarTrack || _fdr.InhibitAutoCouple;
            }
        }

        CanDecouple = canDecouple;
        CanCouple = canCouple;
    }

    // =====================================================================
    //  vatSys events
    // =====================================================================

    // The vatSys events can occur on any thread. The view binds to this view model, thus change it on the UI thread.
    void OnGui(Action action)
    {
        if (_disposed)
            return;

        if (_dispatcher.CheckAccess())
        {
            Protect(action);
            return;
        }

        _dispatcher.BeginInvoke(new Action(() =>
        {
            if (!_disposed)
                Protect(action);
        }));
    }

    // An exception that is not caught on the UI thread stops vatSys
    void Protect(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "An error occurred in the Flight Plan Window");
        }
    }

    void OnFdrPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null && !WatchedProperties.Contains(e.PropertyName))
            return;

        OnGui(() =>
        {
            if (_fdr == null || sender != _fdr || _mode != Mode.View)
                return;

            LoadFieldData(keepEdits: true);
            StampUpdateTime();
            RefreshState();
        });
    }

    void OnFdrsChanged(object sender, FDP2.FDRsChangedEventArgs e)
    {
        var current = _fdr;
        if (current == null || e.Change != current || FDP2.GetFDRs.Contains(current))
            return;

        // The FDR of this window was deleted
        OnGui(() =>
        {
            if (_fdr == current)
                LoadFdr(null);
        });
    }

    void OnValidAtcChanged(object sender, EventArgs e)
    {
        OnGui(() => LoadFdr(null));
    }

    void OnSelectedTrackChanged(object sender, EventArgs e)
    {
        if (_fdr == null)
            return;

        OnGui(() => UpdateCoupleButtons(_mode == Mode.View && !_requestPending));
    }

    // =====================================================================
    //  Mode buttons
    // =====================================================================

    [RelayCommand(CanExecute = nameof(CanCreate))]
    void Create() => ToggleMode(Mode.Create, EnterCreateMode, nameof(IsCreateSelected));

    [RelayCommand(CanExecute = nameof(CanModify))]
    void Modify() => ToggleMode(Mode.Modify, EnterModifyMode, nameof(IsModifySelected));

    [RelayCommand(CanExecute = nameof(CanDepart))]
    void Depart() => ToggleMode(Mode.Depart, EnterDepartMode, nameof(IsDepartSelected));

    // A mode button is a toggle: a click while the mode is on leaves the mode
    void ToggleMode(Mode mode, Action enter, string isSelectedProperty)
    {
        Guard(() =>
        {
            if (_mode == mode)
                LeaveEditMode();
            else
                enter();
        });

        // A toggle button changes its own state when it is clicked. Only RefreshState changes the mode. Thus, send
        // the state again, so that the button shows the mode.
        OnPropertyChanged(isSelectedProperty);
    }

    void EnterCreateMode()
    {
        var acid = Acid.Text.Trim().ToUpperInvariant();
        if (acid == "")
        {
            ShowError(new FDRParseException(FDRParseException.NOT_VALID_CALLSIGN, Acid.Text));
            RefreshState();
            return;
        }

        var sameFlight = _fdr?.Callsign == acid;
        if (sameFlight && _fdr?.HavePermission == false)
        {
            ShowError(new FDRParseException(FDRParseException.CONT_FP_EXISTS, acid));
            RefreshState();
            return;
        }

        var pilots = Network.GetOnlinePilots;
        if (pilots == null || !pilots.Any(p => (p.Callsign ?? "").ToUpperInvariant().Trim() == acid))
        {
            ShowError(new FDRParseException(FDRParseException.CALLSIGN_DOES_NOT_EXIST, acid));
            RefreshState();
            return;
        }

        if (!sameFlight && _fdr != null)
        {
            // A different callsign was typed over a loaded plan. Start from an empty plan, and do not copy the plan
            // of the other aircraft.
            Unsubscribe();
            HideGraphicRoute();
            _fdr = null;
            LoadFieldData(keepEdits: false);
            StampUpdateTime();
        }

        Acid.Set(acid);
        _mode = Mode.Create;
        ClearError();
        RefreshState();

        Etd.Set("");
        Assr.Set("");
        Opdata.Set("");
        Atd.Set("");
        SetDefault(Typ1, "1");
        SetDefault(Frul, "I");
        SetDefault(Equip1, "G");
        SetDefault(Equip2, "C");
        Typ2.Focus();
    }

    static void SetDefault(FieldViewModel field, string value)
    {
        var text = field.Text.Trim();
        if (text == "" || text == "0")
            field.Set(value);
    }

    void EnterModifyMode()
    {
        if (_fdr == null)
        {
            RefreshState();
            return;
        }

        if (!_fdr.HavePermission)
        {
            ShowError(new FDRParseException(FDRParseException.CONT_FP_EXISTS, _fdr.Callsign));
            RefreshState();
            return;
        }

        Acid.Set(_fdr.Callsign);
        LoadFieldData(keepEdits: false);
        _mode = Mode.Modify;
        ClearError();
        RefreshState();
        Rte.Focus();
    }

    void EnterDepartMode()
    {
        if (_fdr == null)
        {
            RefreshState();
            return;
        }

        _mode = Mode.Depart;
        ClearError();
        RefreshState();
        Atd.Focus();
        Atd.SelectAll();
    }

    /// <summary>
    /// Leaves Create, Modify, or Depart, and shows the stored plan again.
    /// </summary>
    void LeaveEditMode()
    {
        ClearError();
        LoadFdr(_fdr != null && FDP2.GetFDRs.Contains(_fdr) ? _fdr : null);
    }

    // =====================================================================
    //  Enter / Escape / Clear
    // =====================================================================

    [RelayCommand(CanExecute = nameof(CanEnter))]
    void Enter() => Guard(EnterPressed);

    [RelayCommand]
    void Escape() => Guard(EscapePressed);

    [RelayCommand]
    void Clear() => Guard(ClearAll);

    void EnterPressed()
    {
        switch (_mode)
        {
            case Mode.Modify:
                CommitModify();
                break;
            case Mode.Create:
                CommitCreate();
                break;
            case Mode.Depart:
                CommitDepart();
                break;
            default:
                EnterInViewMode();
                break;
        }
    }

    void EscapePressed()
    {
        ClearError();
        if (_mode != Mode.View)
        {
            LeaveEditMode();
        }
        else
        {
            var acid = Acid.Text;
            LoadFdr(null);
            Acid.Set(acid);
        }

        Acid.Focus();
    }

    void ClearAll()
    {
        ClearError();
        LoadFdr(null);
        Acid.Set("");
        Acid.Focus();
    }

    static string OneLine(string? text)
    {
        return Regex.Replace(text ?? "", @"\s*[\r\n]+\s*", " ").Trim();
    }

    void CommitModify()
    {
        var fdr = _fdr;
        if (fdr == null)
        {
            LeaveEditMode();
            return;
        }

        if (!fdr.HavePermission)
        {
            ShowError(new FDRParseException(FDRParseException.CONT_FP_EXISTS, "CSEC " + fdr.ControllerTracking?.Callsign));
            return;
        }

        try
        {
            FDP2.ModifyFDR(fdr, fdr.Callsign, Frul.Text, Adep.Text, Ades.Text, OneLine(Rte.Text),
                RemarksWithRegistration(), Typ1.Text, Typ2.Text, Typ3.Text, Equip1.Text, Equip2.Text,
                Tas.Text, Rfl.Text, Etd.Text, Eet.Text, Atd.Text, Altn1.Text);
        }
        catch (FDRParseException ex)
        {
            ShowError(ex);
            return;
        }

        FlightPlanInternals.SendFlightPlanChange(fdr);
        ClearError();
        LoadFdr(fdr);
    }

    void CommitCreate()
    {
        var acid = Acid.Text.Trim().ToUpperInvariant();
        var existed = FDP2.GetFDRIndex(acid) != -1;

        try
        {
            FDP2.CreateFDR(acid, Frul.Text, Adep.Text, Ades.Text, OneLine(Rte.Text), RemarksWithRegistration(),
                Typ1.Text, Typ2.Text, Typ3.Text, Equip1.Text, Equip2.Text, Tas.Text, Rfl.Text,
                Etd.Text, Eet.Text, "", Altn1.Text);
        }
        catch (FDRParseException ex)
        {
            ShowError(ex);
            return;
        }

        var created = FDP2.GetFDRs.LastOrDefault(f => f.Callsign == acid);
        if (created == null)
        {
            ShowError("'" + acid + "' : FDR was not created");
            return;
        }

        if (existed)
            FlightPlanInternals.SendFlightPlanChange(created);
        else
            FlightPlanInternals.SendNewFlightPlan(created);

        // ASSR and OPDATA typed while creating go to the new plan
        string? problem = null;
        var ssr = Assr.Text.Trim();
        var opdata = Opdata.Text.Trim();
        if (ssr != "")
        {
            try
            {
                FDP2.SetASSR(created, Convert.ToInt32(ssr, 8));
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
            {
                problem = ex is ArgumentException ? ex.Message : "Invalid SSR code";
            }
        }

        if (opdata != "")
            FDP2.SetLabelData(created, opdata);

        ClearError();
        LoadFdr(created);
        if (problem != null)
            ShowError(problem);
    }

    void CommitDepart()
    {
        var fdr = _fdr;
        if (fdr == null)
        {
            LeaveEditMode();
            return;
        }

        try
        {
            if (Atd.Text.Length == 0)
            {
                if (fdr.ATD != DateTime.MaxValue)
                    FDP2.CancelFDRDeparture(fdr);
                else
                    FDP2.DepartFDR(fdr, DateTime.UtcNow);
            }
            else
            {
                FDP2.DepartFDR(fdr, Atd.Text);
            }
        }
        catch
        {
            ShowError("Unable to parse ATD");
            return;
        }

        ClearError();
        LoadFdr(fdr);
    }

    void EnterInViewMode()
    {
        var acid = Acid.Text.Trim().ToUpperInvariant();
        if (acid == "")
            return;

        if (_fdr != null && _fdr.Callsign == acid)
        {
            if (CanEditSystemData())
                CommitSystemData();

            return;
        }

        if (FDP2.GetFDRIndex(acid) != -1)
        {
            var found = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == acid);
            if (found != null)
            {
                ClearError();
                LoadFdr(found);
                return;
            }
        }

        RequestFlightPlan(acid);
    }

    /// <summary>
    /// Applies the ASSR and OPDATA changes that were typed in the window.
    /// </summary>
    void CommitSystemData()
    {
        var fdr = _fdr;
        if (fdr == null)
            return;

        if (Assr.IsModified)
        {
            var text = Assr.Text.Trim();
            if (text.Length == 0)
            {
                FDP2.SetASSR(fdr);
            }
            else
            {
                int code;
                try
                {
                    code = Convert.ToInt32(text, 8);
                }
                catch
                {
                    ShowError("Invalid SSR code");
                    return;
                }

                if (fdr.AssignedSSRCode != code)
                {
                    try
                    {
                        FDP2.SetASSR(fdr, code);
                    }
                    catch (ArgumentException ex)
                    {
                        ShowError(ex.Message);
                        return;
                    }
                }
            }
        }

        if (Opdata.IsModified && Opdata.Text != fdr.LabelOpData)
            FDP2.SetLabelData(fdr, Opdata.Text);

        ClearError();
        Assr.IsModified = false;
        Opdata.IsModified = false;
        LoadFieldData(keepEdits: false);
        StampUpdateTime();
        RefreshState();
    }

    /// <summary>
    /// There is no local FDR. Asks the network for the plan, the same as the vatSys window.
    /// </summary>
    void RequestFlightPlan(string acid)
    {
        CancelPendingRequest();
        Unsubscribe();
        HideGraphicRoute();
        _fdr = null;
        LoadFieldData(keepEdits: false);
        ClearError();

        _requestPending = true;
        RefreshState();

        _requestTimer = new DispatcherTimer(DispatcherPriority.Normal, _dispatcher) { Interval = RequestTimeout };
        _requestTimer.Tick += (_, _) => Guard(() => FinishRequest(acid, definitelyMissing: false));

        try
        {
            _unsubscribeNoFlightPlan = FlightPlanInternals.SubscribeNoFlightPlan(callsign =>
            {
                if (callsign != null && callsign.Trim().ToUpperInvariant() == acid)
                    OnGui(() => FinishRequest(acid, definitelyMissing: true));
            });
        }
        catch (Exception ex)
        {
            // Without the event, the timer still finds that the flight plan did not come
            _errorReporter.ReportError(ex, "Cannot find when the network has no flight plan");
        }

        try
        {
            FlightPlanInternals.SendFlightPlanRequest(acid);
        }
        catch
        {
            CancelPendingRequest();
            RefreshState();
            throw;
        }

        _requestTimer.Start();
    }

    void FinishRequest(string acid, bool definitelyMissing)
    {
        if (!_requestPending)
            return;

        CancelPendingRequest();

        var found = definitelyMissing ? null : FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == acid);
        if (found != null)
        {
            ClearError();
            LoadFdr(found);
        }
        else
        {
            RefreshState();
            ShowError("'" + acid + "' : no FDR exists");
        }
    }

    void CancelPendingRequest()
    {
        _requestPending = false;

        _requestTimer?.Stop();
        _requestTimer = null;

        _unsubscribeNoFlightPlan?.Invoke();
        _unsubscribeNoFlightPlan = null;
    }

    // =====================================================================
    //  Registration (REGN <-> REG/ in the remarks)
    // =====================================================================

    internal static string ParseRegistration(string? remarks)
    {
        var match = RegistrationRegex.Match(remarks ?? "");
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : "";
    }

    /// <summary>
    /// The remarks with the REG/ item set to the REGN field.
    /// </summary>
    internal static string ApplyRegistration(string? remarks, string? regn)
    {
        remarks ??= "";
        regn = (regn ?? "").Trim().ToUpperInvariant();
        if (regn == ParseRegistration(remarks))
            return remarks;

        if (regn == "")
            return Regex.Replace(RegistrationRegex.Replace(remarks, ""), @"\s{2,}", " ").Trim();

        if (RegistrationRegex.IsMatch(remarks))
            return RegistrationRegex.Replace(remarks, "REG/" + regn, 1);

        return (remarks.Trim() + " REG/" + regn).Trim();
    }

    string RemarksWithRegistration()
    {
        return ApplyRegistration(OneLine(Other.Text), Regn.Text);
    }

    // =====================================================================
    //  Function buttons
    // =====================================================================

    [RelayCommand(CanExecute = nameof(CanEstimate))]
    void Estimate() => Guard(() =>
    {
        var fdr = _fdr;
        if (fdr == null)
            return;

        if (fdr.ParsedRoute == null || fdr.ParsedRoute.Count == 0)
        {
            ShowError("Cannot activate (NO ROUTE)");
            return;
        }

        FlightPlanInternals.ShowEstimateWindow(fdr);
        ReloadAfterAction(fdr);
    });

    [RelayCommand(CanExecute = nameof(CanBack))]
    void Back() => Guard(() =>
    {
        var fdr = _fdr;
        if (fdr == null)
            return;

        FDP2.BackFDR(fdr);
        if (fdr.ParsedRoute != null && fdr.ParsedRoute.Count > 0)
            FlightPlanInternals.SendCancelEstimate(fdr, fdr.ParsedRoute[0]);

        ReloadAfterAction(fdr);
    });

    [RelayCommand(CanExecute = nameof(CanInhibit))]
    void Inhibit() => Guard(() =>
    {
        var fdr = _fdr;
        if (fdr == null)
            return;

        if (fdr.State == States.STATE_INHIBITED)
            MMI.UnInhibit(fdr);
        else
            MMI.Inhibit(fdr);

        ReloadAfterAction(fdr);
    });

    [RelayCommand(CanExecute = nameof(CanCancel))]
    void Cancel() => Guard(() =>
    {
        var fdr = _fdr;
        if (fdr == null)
            return;

        if (!fdr.HavePermission && fdr.IsTracked)
        {
            ShowError(new FDRParseException(FDRParseException.CONT_FP_EXISTS, fdr.Callsign));
            return;
        }

        var message = fdr.State <= States.STATE_COORDINATED
            ? "This will deactivate the FDR for " + fdr.Callsign + "."
            : "This will finish the FDR for " + fdr.Callsign + ".";
        if (FlightPlanInternals.Confirm("Cancel " + fdr.Callsign + "?", message))
            FDP2.CancelFDR(fdr, sendEst: true);

        ReloadAfterAction(fdr);
    });

    [RelayCommand(CanExecute = nameof(CanAccept))]
    void Accept() => Guard(() =>
    {
        if (_fdr != null)
            MMI.AcceptJurisdiction(_fdr);
    });

    [RelayCommand(CanExecute = nameof(CanHandoff))]
    void Handoff() => Guard(() =>
    {
        if (_fdr != null)
            MMI.OpenHandoffMenu(_fdr);
    });

    [RelayCommand(CanExecute = nameof(CanPrl))]
    void Prl() => Guard(() =>
    {
        var fdr = _fdr;
        if (fdr == null)
            return;

        if (fdr.CoupledTrack != null)
        {
            ShowError("PRL not available (track coupled)");
            return;
        }

        ClearError();
        MMI.OpenPRLMenu(fdr, System.Windows.Forms.Cursor.Position);
    });

    [RelayCommand(CanExecute = nameof(CanCfl))]
    void Cfl() => Guard(() =>
    {
        var fdr = _fdr;
        if (fdr == null)
            return;

        var track = MMI.FindTrack(fdr);
        if (track == null)
        {
            ShowError("CFL not available (no track)");
            return;
        }

        ClearError();
        MMI.OpenCFLMenu(track, System.Windows.Forms.Cursor.Position);
    });

    /// <summary>
    /// NCOU: breaks the radar couple, and stops the automatic couple.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDecouple))]
    void Decouple() => Guard(() =>
    {
        var fdr = _fdr;
        if (fdr?.CoupledTrack == null)
            return;

        RDP.DeCouple(fdr.CoupledTrack);
        fdr.InhibitAutoCouple = true;
        RefreshState();
    });

    /// <summary>
    /// BCOU: couples to the selected radar track, or gives the couple back to the system.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCouple))]
    void Couple() => Guard(() =>
    {
        var fdr = _fdr;
        if (fdr == null || fdr.CoupledTrack != null)
            return;

        var selected = MMI.SelectedTrack;
        if (selected != null && selected.Type == Track.TrackTypes.TRACK_TYPE_RADAR && selected.GetFDR() == null)
        {
            RDP.Couple(fdr, selected.GetRadarTrack());
            fdr.InhibitAutoCouple = true;
            MMI.DeselectTrack();
        }
        else if (fdr.InhibitAutoCouple)
        {
            fdr.InhibitAutoCouple = false;
        }
        else
        {
            ShowError("Invalid / no radar track selected");
        }

        RefreshState();
    });

    [RelayCommand(CanExecute = nameof(CanToggleRoute))]
    void ToggleRoute() => Guard(() =>
    {
        if (_routeTrack != null)
        {
            HideGraphicRoute();
            return;
        }

        if (_fdr == null)
            return;

        var track = MMI.FindTrack(_fdr);
        if (track == null)
        {
            ShowError("No track to display route for");
            return;
        }

        ClearError();
        MMI.ShowGraphicRoute(track);
        _routeTrack = track;
    });

    void HideGraphicRoute()
    {
        if (_routeTrack == null)
            return;

        try
        {
            MMI.HideGraphicRoute(_routeTrack);
        }
        catch
        {
            // The track can go before the route is hidden
        }

        _routeTrack = null;
    }

    void ReloadAfterAction(FDP2.FDR fdr)
    {
        if (_fdr == fdr && _mode == Mode.View)
        {
            LoadFieldData(keepEdits: true);
            StampUpdateTime();
            RefreshState();
        }
    }

    // =====================================================================
    //  Messages
    // =====================================================================

    /// <summary>
    /// Does an action. A failure shows in the message area, and does not stop vatSys.
    /// </summary>
    void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (FDRParseException ex)
        {
            ShowError(ex);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            _errorReporter.ReportError(ex, "An error occurred in the Flight Plan Window");
            try
            {
                RefreshState();
            }
            catch
            {
                // The state is already shown in the message area
            }
        }
    }

    void ShowError(FDRParseException ex)
    {
        ShowError(string.IsNullOrEmpty(ex.OffendingElement) ? ex.Cause : "'" + ex.OffendingElement + "' : " + ex.Cause);
    }

    void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        Acid.HasError = true;
    }

    void ClearError()
    {
        if (!HasError)
            return;

        ErrorMessage = "";
        HasError = false;
        Acid.HasError = false;
    }

    // =====================================================================
    //  Teardown
    // =====================================================================

    /// <summary>
    /// Stops listening to vatSys. The window calls this method when it closes.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        Unsubscribe();
        CancelPendingRequest();
        HideGraphicRoute();

        FDP2.FDRsChanged -= OnFdrsChanged;
        Network.ValidATCChanged -= OnValidAtcChanged;
        MMI.SelectedTrackChanged -= OnSelectedTrackChanged;
    }
}
