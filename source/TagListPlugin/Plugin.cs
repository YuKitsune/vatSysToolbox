using System.ComponentModel.Composition;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using TagListPlugin.ViewModels;
using TagListPlugin.Views;
using vatsys;
using vatsys.Plugin;
using vatSysToolbox;
using vatSysToolbox.Extensions;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

namespace TagListPlugin;

/// <summary>
/// Shows the quick tags in a list. Other plugins can show the list, and add or remove quick tags, with
/// <see cref="TagListMessages"/>.
/// </summary>
[Export(typeof(IPlugin))]
public class Plugin : IPlugin
{
    public const string Name = "Tag List";

    const string WindowKey = "TagList";
    const string WindowTitle = "Tag List";

    // The messages that the plugin sends have no data
    static readonly MessageContent EmptyContent = new("application/json", "{}"u8.ToArray());

    readonly IGuiInvoker _guiInvoker;
    readonly IErrorReporter _errorReporter;
    readonly IPluginMessenger _messenger;
    readonly WindowManager _windowManager;
    readonly ToolStripMenuItem _tagListMenuItem;

    string IPlugin.Name => Name;

    public Plugin()
    {
        DpiAwareness.EnsureDpiAwareness();

        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddVatSysToolbox(Name)
            .BuildServiceProvider();

        _guiInvoker = serviceProvider.GetRequiredService<IGuiInvoker>();
        _errorReporter = serviceProvider.GetRequiredService<IErrorReporter>();
        _messenger = serviceProvider.GetRequiredService<IPluginMessenger>();
        _windowManager = serviceProvider.GetRequiredService<WindowManager>();

        _tagListMenuItem = AddMenuItem("Tag List", ToggleTagList);
        AddMenuItem("Rad Tag", AddQuickTag);
        AddMenuItem("Del Tag", RemoveQuickTag);

        HandleMessage(TagListMessages.Toggle, ToggleTagList);
        HandleMessage(TagListMessages.AddQuickTag, AddQuickTag);
        HandleMessage(TagListMessages.RemoveQuickTag, RemoveQuickTag);
        _messenger.Start();

        // vatSys does not send events for the quick tags and the radar tracks. Thus, the patches send the events.
        try
        {
            VatSysPatches.Apply(_errorReporter);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Cannot send the vatSys events. The Tag List does not show the changes.");
        }
    }

    void OpenTagList()
    {
        _windowManager.TryCreateWindow(
            WindowKey,
            WindowTitle,
            windowHandle =>
            {
                var viewModel = new TagListViewModel(WeakReferenceMessenger.Default, _guiInvoker, _errorReporter);
                windowHandle.Closed += (_, _) =>
                {
                    WeakReferenceMessenger.Default.UnregisterAll(viewModel);
                    ShowState(isOpen: false);
                };

                ShowState(isOpen: true);
                return new TagList(viewModel);
            },
            sizeToContent: false,
            canClose: false,
            canMinimise: false);
    }

    // Adds an item to the vatSys Tools menu
    static ToolStripMenuItem AddMenuItem(string text, Action click)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => click();

        MMI.AddCustomMenuItem(new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            CustomToolStripMenuItemCategory.Tools,
            item));

        return item;
    }

    // The messenger receives the messages on a background thread. The actions go to the UI thread themselves.
    void HandleMessage(string type, Action action)
    {
        _messenger.Subscribe(type, (_, _) =>
        {
            action();
            return Task.CompletedTask;
        });
    }

    // These actions can come from a menu item on the UI thread, or from a message on a background thread. The GUI
    // invoker does the action on the UI thread, and reports the exceptions, because an exception that is not caught on
    // the UI thread stops vatSys.

    void ToggleTagList()
    {
        _guiInvoker.InvokeOnGUI(_ =>
        {
            // The window manager knows if the window is open
            if (_windowManager.TryGetWindow(WindowKey, out _))
                _windowManager.CloseWindow(WindowKey);
            else
                OpenTagList();
        });
    }

    // Shows the state in the Tools menu, and tells the other plugins
    void ShowState(bool isOpen)
    {
        _tagListMenuItem.Checked = isOpen;
        _ = SendState(isOpen ? TagListMessages.Opened : TagListMessages.Closed);
    }

    void AddQuickTag() => _guiInvoker.InvokeOnGUI(QuickTagHelpers.OpenQuickTagWindow);

    void RemoveQuickTag() => _guiInvoker.InvokeOnGUI(_ => QuickTagHelpers.RemoveSelectedQuickTag());

    // Tells the other plugins (for example, the Tag List button of the Eurocat Menus plugin) that the Tag List is shown
    // or hidden
    async Task SendState(string messageType)
    {
        try
        {
            await _messenger.Broadcast(messageType, EmptyContent);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Cannot send the state of the Tag List");
        }
    }

    public void OnFDRUpdate(FDP2.FDR updated) { }

    public void OnRadarTrackUpdate(RDP.RadarTrack updated) { }
}
