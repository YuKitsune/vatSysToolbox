using System.ComponentModel.Composition;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Messaging;
using EurocatMenusPlugin.Configuration;
using EurocatMenusPlugin.FlightPlan;
using EurocatMenusPlugin.Shims;
using EurocatMenusPlugin.ViewModels;
using EurocatMenusPlugin.Views;
using Microsoft.Extensions.DependencyInjection;
using vatsys;
using vatsys.Plugin;
using vatSysToolbox;
using vatSysToolbox.Extensions;
using vatSysToolbox.Infrastructure;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin;

[Export(typeof(IPlugin))]
public class Plugin : IPlugin
{
#if DEBUG
    public const string Name = "Eurocat Menus - Debug";
#else
    public const string Name = "Eurocat Menus";
#endif

    /// <summary>
    /// The name that other plugins use to send messages to this plugin.
    /// </summary>
    public const string MailboxName = "dev.eoinmotherway.eurocatmenus";

    static readonly TimeSpan DoubleAltPressInterval = TimeSpan.FromSeconds(1);

    // The text of the item in the vatSys Settings menu that shows the menus again
    const string ShowMenusItemText = "Eurocat Menus";

    // The text of the item in the vatSys Windows menu that shows or hides the menus
    const string ToggleMenusItemText = "Eurocat Menus";

    // The key of the vatSys main form in Application.OpenForms
    const string VatSysMainFormKey = "MainForm";

    // The text of the item in the vatSys Settings menu that selects the Flight Plan Window of this plugin
    const string FlightPlanWindowItemText = "Eurocat Flight Plan Window";

    readonly IServiceProvider _serviceProvider;
    readonly FlightPlanWindowSettings _flightPlanWindowSettings;
    readonly FlightPlanWindow _flightPlanWindow;

    // The patch that opens this menu is applied before the menus are added. Thus, the menu is enabled only when the
    // menus show.
    readonly CallsignMenu _callsignMenu;

    string IPlugin.Name => Name;

    public Plugin()
    {
        DpiAwareness.EnsureDpiAwareness();

        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddErrorReporter(Name)
            .AddWindowManager()
            .AddPluginMessenger(MailboxName)
            .BuildServiceProvider();

        var errorReporter = _serviceProvider.GetRequiredService<IErrorReporter>();
        _callsignMenu = new CallsignMenu(errorReporter);
        try
        {
            VatSysPatches.Apply(errorReporter, _callsignMenu);
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, "Cannot send the vatSys events. The menus do not show the changes.");
        }

        // Read the selection before the patches apply, so that the first window that opens is the selected window
        _flightPlanWindowSettings = new FlightPlanWindowSettings(FlightPlanWindowSettings.DefaultFilePath, errorReporter);
        FlightPlanWindowPatches.IsEnabled = _flightPlanWindowSettings.Load();

        _flightPlanWindow = new FlightPlanWindow(
            _serviceProvider.GetRequiredService<WindowManager>(),
            errorReporter);

        try
        {
            FlightPlanWindowPatches.Apply(errorReporter, _flightPlanWindow.Open);
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, "Cannot replace the vatSys Flight Plan Window. The vatSys window shows.");
        }

        // vatSys loads the plugins while it creates the main form. The main form is ready only after the message
        // loop starts, thus replace the menu bar when the message loop is idle for the first time.
        Application.Idle += OnFirstIdle;
    }

    void OnFirstIdle(object sender, EventArgs e)
    {
        Application.Idle -= OnFirstIdle;

        // An exception that is not caught on the UI thread stops vatSys.
        try
        {
            ReplaceMenuBar();
        }
        catch (Exception ex)
        {
            _serviceProvider.GetRequiredService<IErrorReporter>().ReportError(ex, "Cannot replace the vatSys menu bar");
        }

        // Load the Flight Plan Window once now, so that the first window opens as fast as the next windows
        try
        {
            _flightPlanWindow.WarmUp();
        }
        catch (Exception ex)
        {
            _serviceProvider.GetRequiredService<IErrorReporter>().ReportError(ex, "Cannot load the Flight Plan Window");
        }
    }

    void ReplaceMenuBar()
    {
        var guiInvoker = _serviceProvider.GetRequiredService<IGuiInvoker>();
        var errorReporter = _serviceProvider.GetRequiredService<IErrorReporter>();
        var clock = _serviceProvider.GetRequiredService<IClock>();
        var messenger = _serviceProvider.GetRequiredService<IPluginMessenger>();
        var configuration = LoadConfiguration(errorReporter);

        // vatSys raises Application.Idle on the UI thread. The message filter must be added on the UI thread, because it
        // only receives the messages of that thread.
        var mainForm = Application.OpenForms[VatSysMainFormKey]
            ?? throw new InvalidOperationException("Cannot find the vatSys main form");

        var vatSysMenu = new VatSysMenu(
            mainForm.MainMenuStrip ?? throw new InvalidOperationException("Cannot find the vatSys menu bar"),
            errorReporter);
        var vatSysInternals = new VatSysInternals(mainForm);
        var menuBar = new MenuBarLayout(mainForm);

        var menuEvents = new MenuEvents(messenger, guiInvoker, errorReporter);

        // Make this before the menus are added, so that it sends the event when the vatSys menu bar is hidden
        var builtInEvents = new BuiltInEvents(menuEvents, menuBar, guiInvoker);

        var vatSysMessenger = WeakReferenceMessenger.Default;
        var topMenuViewModel = new TopMenuViewModel(
            vatSysMenu,
            vatSysInternals,
            clock,
            vatSysMessenger,
            guiInvoker,
            errorReporter);

        // The toggle buttons subscribe to their events. Thus, start the messenger after the items are made.
        var bottomMenuItemFactory = new BottomMenuItemFactory(
            vatSysMenu,
            vatSysInternals,
            menuEvents,
            messenger,
            errorReporter);
        var bottomMenuItems = bottomMenuItemFactory.CreateItems(configuration.BottomMenu);
        var bottomMenuViewModel = new BottomMenuViewModel(bottomMenuItems);

        // The shims do the work of other plugins until those plugins handle their own messages. They subscribe to
        // their messages, thus make them before the messenger starts.
        _ = new CpdlcShim(vatSysMenu, menuEvents, messenger, vatSysMessenger, guiInvoker);
        _ = new MaestroShim(vatSysMenu, messenger, guiInvoker);
        _ = new AtisShim(vatSysMenu, messenger, guiInvoker);

        // The built-in message handlers, the Strip Setup window, and full screen also subscribe to their messages
        _ = new BuiltInMessageHandlers(vatSysMenu, menuBar, messenger, guiInvoker);
        _ = new StripSetupWindow(vatSysMenu, menuEvents, messenger, vatSysMessenger, guiInvoker);
        _ = new FullScreenMode(mainForm, menuEvents, messenger, guiInvoker);
        var flightPlanWindowSwitch = new FlightPlanWindowSwitch(
            _flightPlanWindowSettings,
            menuEvents,
            messenger,
            guiInvoker);

        messenger.Start();
        builtInEvents.SendCurrentState();
        flightPlanWindowSwitch.SendCurrentState();

        menuBar.AddMenus(new TopMenu(topMenuViewModel), new BottomMenu(bottomMenuViewModel));

        // When the menus are hidden, use the built-in aircraft menu
        _callsignMenu.IsEnabled = menuBar.AreMenusVisible;
        menuBar.AreMenusVisibleChanged += (_, _) => _callsignMenu.IsEnabled = menuBar.AreMenusVisible;

        var doubleAltPressFilter = new DoubleAltPressFilter(clock, DoubleAltPressInterval, () =>
        {
            // An exception that is not caught on the UI thread stops vatSys.
            try
            {
                menuBar.ToggleVatSysMenu();
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, "Cannot show or hide the vatSys menu bar");
            }
        });

        Application.AddMessageFilter(doubleAltPressFilter);

        AddShowMenusItem(menuBar, errorReporter);
        AddToggleMenusItem(menuBar, errorReporter);
        AddFlightPlanWindowItem(flightPlanWindowSwitch, errorReporter);
    }

    // Selects the Flight Plan Window of this plugin or the vatSys window, from the vatSys Settings menu. Thus, the
    // selection is also available when the menus of this plugin are hidden. The check mark shows if the window of this
    // plugin is selected.
    static void AddFlightPlanWindowItem(FlightPlanWindowSwitch flightPlanWindowSwitch, IErrorReporter errorReporter)
    {
        var item = new ToolStripMenuItem(FlightPlanWindowItemText)
        {
            Checked = flightPlanWindowSwitch.IsEurocatWindowEnabled
        };
        flightPlanWindowSwitch.IsEurocatWindowEnabledChanged +=
            (_, _) => item.Checked = flightPlanWindowSwitch.IsEurocatWindowEnabled;
        item.Click += (_, _) =>
        {
            // An exception that is not caught on the UI thread stops vatSys.
            try
            {
                flightPlanWindowSwitch.Toggle();
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, "Cannot select the Flight Plan Window");
            }
        };

        MMI.AddCustomMenuItem(new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            CustomToolStripMenuItemCategory.Settings,
            item));
    }

    // Shows or hides the menus from the vatSys Windows menu. The check mark shows if the menus show.
    static void AddToggleMenusItem(MenuBarLayout menuBar, IErrorReporter errorReporter)
    {
        var item = new ToolStripMenuItem(ToggleMenusItemText) { Checked = menuBar.AreMenusVisible };
        menuBar.AreMenusVisibleChanged += (_, _) => item.Checked = menuBar.AreMenusVisible;
        item.Click += (_, _) =>
        {
            // An exception that is not caught on the UI thread stops vatSys.
            try
            {
                menuBar.ToggleMenus();
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, "Cannot show or hide the menus");
            }
        };

        MMI.AddCustomMenuItem(new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            CustomToolStripMenuItemCategory.Windows,
            item));
    }

    // When the menus are hidden, the vatSys menu bar shows. This item shows the menus again.
    static void AddShowMenusItem(MenuBarLayout menuBar, IErrorReporter errorReporter)
    {
        var item = new ToolStripMenuItem(ShowMenusItemText);
        item.Click += (_, _) =>
        {
            // An exception that is not caught on the UI thread stops vatSys.
            try
            {
                menuBar.ShowMenus();
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, "Cannot show the menus");
            }
        };

        MMI.AddCustomMenuItem(new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            CustomToolStripMenuItemCategory.Settings,
            item));
    }

    // If the configuration cannot be read, the bottom menu has no items
    static MenuConfiguration LoadConfiguration(IErrorReporter errorReporter)
    {
        try
        {
            return MenuConfigurationLoader.Load();
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, $"Cannot read {MenuConfigurationLoader.FileName}");
            return new MenuConfiguration();
        }
    }

    public void OnFDRUpdate(FDP2.FDR updated) { }

    public void OnRadarTrackUpdate(RDP.RadarTrack updated) { }
}
