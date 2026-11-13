using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace EurocatMenusPlugin;

/// <summary>
/// Hides the vatSys menu bar. Puts the top menu at the top of the main form, and the bottom menu at the bottom.
/// The views set the height of the menus. The ASD fills the space between the menus.
/// The vatSys menu bar can show again above the top menu. The ASD then moves down to make space for it.
/// The menus can also be hidden. Then the vatSys menu bar and the ASD are the same as when the plugin is not loaded.
/// </summary>
/// <remarks>
/// The menus can show and hide the vatSys menu bar. Thus, create this class first, and then add the menus with
/// <see cref="AddMenus"/>.
/// </remarks>
public class MenuBarLayout
{
    // The name of the ASD control on the vatSys main form
    const string AsdControlName = "ASD";

    readonly Form _mainForm;
    readonly MenuStrip _vatSysMenu;
    readonly Control _asd;

    ElementHost? _topMenuHost;
    ElementHost? _bottomMenuHost;

    public MenuBarLayout(Form mainForm)
    {
        _mainForm = mainForm;

        _vatSysMenu = mainForm.MainMenuStrip
            ?? throw new InvalidOperationException("Cannot find the vatSys menu bar");

        _asd = mainForm.Controls[AsdControlName]
            ?? throw new InvalidOperationException("Cannot find the ASD");
    }

    /// <summary>
    /// <c>true</c> if the vatSys menu bar shows above the top menu.
    /// </summary>
    public bool IsVatSysMenuVisible { get; private set; } = true;

    public event EventHandler? IsVatSysMenuVisibleChanged;

    /// <summary>
    /// <c>true</c> if the top menu and the bottom menu show.
    /// </summary>
    public bool AreMenusVisible { get; private set; }

    /// <summary>
    /// Adds the menus, and shows them.
    /// </summary>
    public void AddMenus(FrameworkElement topMenu, FrameworkElement bottomMenu)
    {
        _topMenuHost = new ElementHost
        {
            Child = topMenu,
            Location = new System.Drawing.Point(0, 0),
            Size = new System.Drawing.Size(_mainForm.ClientSize.Width, GetHeight(topMenu)),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Visible = false
        };

        _mainForm.Controls.Add(_topMenuHost);

        var bottomMenuHeight = GetHeight(bottomMenu);
        _bottomMenuHost = new ElementHost
        {
            Child = bottomMenu,
            Location = new System.Drawing.Point(0, _asd.Bottom - bottomMenuHeight),
            Size = new System.Drawing.Size(_mainForm.ClientSize.Width, bottomMenuHeight),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Visible = false
        };

        _mainForm.Controls.Add(_bottomMenuHost);

        ShowMenus();
    }

    /// <summary>
    /// Shows the menus, and hides the vatSys menu bar. If the menus show, this method does nothing.
    /// </summary>
    public void ShowMenus()
    {
        var (topMenuHost, bottomMenuHost) = GetMenuHosts();
        if (AreMenusVisible)
            return;

        topMenuHost.Top = 0;
        topMenuHost.Visible = true;
        bottomMenuHost.Visible = true;
        _asd.SetBounds(_asd.Left, topMenuHost.Height, _asd.Width, bottomMenuHost.Top - topMenuHost.Height);
        SetVatSysMenuVisible(false);

        AreMenusVisible = true;
    }

    /// <summary>
    /// Hides the menus, and shows the vatSys menu bar. The ASD fills the space below the vatSys menu bar. If the menus
    /// are hidden, this method does nothing.
    /// </summary>
    public void HideMenus()
    {
        var (topMenuHost, bottomMenuHost) = GetMenuHosts();
        if (!AreMenusVisible)
            return;

        topMenuHost.Visible = false;
        bottomMenuHost.Visible = false;
        _asd.SetBounds(_asd.Left, _vatSysMenu.Height, _asd.Width, bottomMenuHost.Bottom - _vatSysMenu.Height);
        SetVatSysMenuVisible(true);

        AreMenusVisible = false;
    }

    /// <summary>
    /// Shows or hides the vatSys menu bar above the top menu. If the menus are hidden, the vatSys menu bar must show.
    /// Thus, this method then does nothing.
    /// </summary>
    public void ToggleVatSysMenu()
    {
        var (topMenuHost, _) = GetMenuHosts();
        if (!AreMenusVisible)
            return;

        if (IsVatSysMenuVisible)
            HideVatSysMenu(topMenuHost);
        else
            ShowVatSysMenu(topMenuHost);
    }

    void ShowVatSysMenu(ElementHost topMenuHost)
    {
        var offset = _vatSysMenu.Height;

        topMenuHost.Top += offset;
        _asd.SetBounds(_asd.Left, _asd.Top + offset, _asd.Width, _asd.Height - offset);
        SetVatSysMenuVisible(true);
    }

    void HideVatSysMenu(ElementHost topMenuHost)
    {
        var offset = _vatSysMenu.Height;

        topMenuHost.Top -= offset;
        _asd.SetBounds(_asd.Left, _asd.Top - offset, _asd.Width, _asd.Height + offset);
        SetVatSysMenuVisible(false);
    }

    void SetVatSysMenuVisible(bool visible)
    {
        _vatSysMenu.Visible = visible;
        if (IsVatSysMenuVisible == visible)
            return;

        IsVatSysMenuVisible = visible;
        IsVatSysMenuVisibleChanged?.Invoke(this, EventArgs.Empty);
    }

    (ElementHost TopMenuHost, ElementHost BottomMenuHost) GetMenuHosts()
    {
        if (_topMenuHost is null || _bottomMenuHost is null)
            throw new InvalidOperationException("The menus are not added");

        return (_topMenuHost, _bottomMenuHost);
    }

    static int GetHeight(FrameworkElement view)
    {
        view.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        return (int)Math.Ceiling(view.DesiredSize.Height);
    }
}
