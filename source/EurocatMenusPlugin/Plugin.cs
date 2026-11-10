using System.ComponentModel.Composition;
using System.Windows.Forms;
using EurocatMenusPlugin.Configuration;
using EurocatMenusPlugin.Shims;
using EurocatMenusPlugin.ViewModels;
using EurocatMenusPlugin.Views;
using Microsoft.Extensions.DependencyInjection;
using vatsys;
using vatsys.Plugin;
using vatSysToolbox;
using vatSysToolbox.Extensions;
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

    static readonly TimeSpan DoubleAltPressInterval = TimeSpan.FromSeconds(1);
    static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    readonly IServiceProvider _serviceProvider;

    System.Windows.Forms.Timer? _refreshTimer;

    string IPlugin.Name => Name;

    public Plugin()
    {
        DpiAwareness.EnsureDpiAwareness();

        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddVatSysToolbox(Name)
            .BuildServiceProvider();

        // vatSys loads the plugins while it creates the main form. The main form is ready only after the message
        // loop starts, thus replace the menu bar when the message loop is idle for the first time.
        Application.Idle += OnFirstIdle;
    }

    void OnFirstIdle(object sender, EventArgs e)
    {
        Application.Idle -= OnFirstIdle;
        ReplaceMenuBar();
    }

    void ReplaceMenuBar()
    {
        var guiInvoker = _serviceProvider.GetRequiredService<IGuiInvoker>();
        var errorReporter = _serviceProvider.GetRequiredService<IErrorReporter>();
        var clock = _serviceProvider.GetRequiredService<IClock>();
        var messenger = _serviceProvider.GetRequiredService<IPluginMessenger>();
        var configuration = LoadConfiguration(errorReporter);

        // The message filter must be added on the UI thread, because it only receives the messages of that thread.
        guiInvoker.InvokeOnGUI(mainForm =>
        {
            var vatSysMenu = new VatSysMenu(
                mainForm.MainMenuStrip ?? throw new InvalidOperationException("Cannot find the vatSys menu bar"),
                errorReporter);
            var vatSysInternals = new VatSysInternals(mainForm);
            var menuBar = new MenuBarLayout(mainForm);

            var menuEvents = new MenuEvents(messenger, guiInvoker, errorReporter);

            // Make this before the menus are added, so that it sends the event when the vatSys menu bar is hidden
            var builtInEvents = new BuiltInEvents(menuEvents, menuBar, guiInvoker);

            var topMenuViewModel = new TopMenuViewModel(vatSysMenu, vatSysInternals, clock, errorReporter);

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
            var cpdlcShim = new CpdlcShim(vatSysMenu, menuEvents, messenger, guiInvoker, errorReporter);
            var holdShim = new HoldShim(vatSysMenu, menuEvents, messenger, guiInvoker, errorReporter);
            _ = new MaestroShim(vatSysMenu, messenger, guiInvoker);
            _ = new AtisShim(vatSysMenu, messenger, guiInvoker);

            // The built-in message handlers, the Strip Setup window, and full screen also subscribe to their messages
            _ = new BuiltInMessageHandlers(vatSysMenu, menuBar, messenger, guiInvoker);
            var stripSetupWindow = new StripSetupWindow(vatSysMenu, menuEvents, messenger, guiInvoker, errorReporter);
            _ = new FullScreenMode(mainForm, menuEvents, messenger, guiInvoker);

            messenger.Start();
            builtInEvents.SendCurrentState();

            menuBar.AddMenus(new TopMenu(topMenuViewModel), new BottomMenu(bottomMenuViewModel));

            // vatSys does not send events for most of the states that the menus show. Thus, read the vatSys state again
            // at an interval.
            void RefreshMenus()
            {
                // An exception that is not caught on the UI thread stops vatSys.
                try
                {
                    topMenuViewModel.Refresh();
                    bottomMenuViewModel.Refresh();
                    stripSetupWindow.Refresh();
                    cpdlcShim.Refresh();
                    holdShim.Refresh();
                }
                catch (Exception ex)
                {
                    errorReporter.ReportError(ex, "Cannot update the menus");
                }
            }

            RefreshMenus();
            _refreshTimer = new System.Windows.Forms.Timer { Interval = (int)RefreshInterval.TotalMilliseconds };
            _refreshTimer.Tick += (_, _) => RefreshMenus();
            _refreshTimer.Start();

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
        });
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
