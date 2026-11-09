using System.ComponentModel.Composition;
using System.Windows.Forms;
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

    readonly IServiceProvider _serviceProvider;

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
            var menuBar = new MenuBarLayout(mainForm);

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
