using System.ComponentModel.Composition;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Messaging;
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

    /// <summary>
    /// The name that other plugins use to send messages to this plugin.
    /// </summary>
    public const string MailboxName = "dev.eoinmotherway.eurocatmenus";

    static readonly TimeSpan DoubleAltPressInterval = TimeSpan.FromSeconds(1);

    // The text of the item in the vatSys Settings menu that shows the menus again
    const string ShowMenusItemText = "Eurocat Menus";

    readonly IServiceProvider _serviceProvider;

    string IPlugin.Name => Name;

    public Plugin()
    {
        DpiAwareness.EnsureDpiAwareness();

        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddVatSysToolbox(Name, MailboxName)
            .BuildServiceProvider();

        var errorReporter = _serviceProvider.GetRequiredService<IErrorReporter>();
        try
        {
            VatSysPatches.Apply(errorReporter);
        }
        catch (Exception ex)
        {
            errorReporter.ReportError(ex, "Cannot send the vatSys events. The menus do not show the changes.");
        }

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

            messenger.Start();
            builtInEvents.SendCurrentState();

            menuBar.AddMenus(new TopMenu(topMenuViewModel), new BottomMenu(bottomMenuViewModel));

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
        });
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
