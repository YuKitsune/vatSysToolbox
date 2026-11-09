using System.ComponentModel.Composition;
using System.Windows.Forms;
using EurocatMenusPlugin.ViewModels;
using EurocatMenusPlugin.Views;
using Microsoft.Extensions.DependencyInjection;
using vatsys;
using vatsys.Plugin;
using vatSysToolbox;
using vatSysToolbox.Extensions;
using vatSysToolbox.Windows;

namespace EurocatMenusPlugin;

[Export(typeof(IPlugin))]
public class Plugin : IPlugin
{
    public const string Name = "Eurocat Menus";

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

        // The message filter must be added on the UI thread, because it only receives the messages of that thread.
        guiInvoker.InvokeOnGUI(mainForm =>
        {
            var vatSysMenu = new VatSysMenu(
                mainForm.MainMenuStrip ?? throw new InvalidOperationException("Cannot find the vatSys menu bar"),
                errorReporter);
            var vatSysInternals = new VatSysInternals(mainForm);
            var menuBar = new MenuBarLayout(mainForm);
            var fullScreenMode = new FullScreenMode(mainForm);

            var topMenuViewModel = new TopMenuViewModel(vatSysMenu, vatSysInternals, clock, errorReporter);
            var bottomMenuViewModel = new BottomMenuViewModel(
                vatSysMenu,
                vatSysInternals,
                menuBar,
                fullScreenMode,
                errorReporter);

            menuBar.AddMenus(new TopMenu(topMenuViewModel), new BottomMenu(bottomMenuViewModel));

            var tagListWindow = new TagListWindow(_serviceProvider.GetRequiredService<WindowManager>(), errorReporter);

            // vatSys does not send events for most of the states that the menus and the Tag List show. Thus, read the
            // vatSys state again at an interval.
            void Refresh()
            {
                // An exception that is not caught on the UI thread stops vatSys.
                try
                {
                    topMenuViewModel.Refresh();
                    bottomMenuViewModel.Refresh();
                }
                catch (Exception ex)
                {
                    errorReporter.ReportError(ex, "Cannot update the menus");
                }

                try
                {
                    tagListWindow.Refresh();
                }
                catch (Exception ex)
                {
                    errorReporter.ReportError(ex, "Cannot update the Tag List");
                }
            }

            Refresh();
            _refreshTimer = new System.Windows.Forms.Timer { Interval = (int)RefreshInterval.TotalMilliseconds };
            _refreshTimer.Tick += (_, _) => Refresh();
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

    public void OnFDRUpdate(FDP2.FDR updated) { }

    public void OnRadarTrackUpdate(RDP.RadarTrack updated) { }
}
