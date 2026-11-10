using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace EurocatMenusPlugin;

/// <summary>
/// Hides the vatSys menu bar. Puts the top menu at the top of the main form, and the bottom menu at the bottom.
/// The views set the height of the menus. The ASD fills the space between the menus.
/// The vatSys menu bar can show again above the top menu. The ASD then moves down to make space for it.
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
    /// Adds the menus, and hides the vatSys menu bar.
    /// </summary>
    public void AddMenus(FrameworkElement topMenu, FrameworkElement bottomMenu)
    {
        var topMenuHeight = GetHeight(topMenu);
        _topMenuHost = new ElementHost
        {
            Child = topMenu,
            Location = new System.Drawing.Point(0, 0),
            Size = new System.Drawing.Size(_mainForm.ClientSize.Width, topMenuHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _mainForm.Controls.Add(_topMenuHost);

        var bottomMenuHeight = GetHeight(bottomMenu);
        var bottomMenuHost = new ElementHost
        {
            Child = bottomMenu,
            Location = new System.Drawing.Point(0, _asd.Bottom - bottomMenuHeight),
            Size = new System.Drawing.Size(_mainForm.ClientSize.Width, bottomMenuHeight),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _mainForm.Controls.Add(bottomMenuHost);

        _asd.SetBounds(_asd.Left, topMenuHeight, _asd.Width, bottomMenuHost.Top - topMenuHeight);

        _vatSysMenu.Visible = false;
        IsVatSysMenuVisible = false;
        IsVatSysMenuVisibleChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleVatSysMenu()
    {
        var topMenuHost = _topMenuHost
            ?? throw new InvalidOperationException("The menus are not added");

        if (IsVatSysMenuVisible)
            HideVatSysMenu(topMenuHost);
        else
            ShowVatSysMenu(topMenuHost);
    }

    void ShowVatSysMenu(ElementHost topMenuHost)
    {
        var offset = _vatSysMenu.Height;

        _vatSysMenu.Visible = true;
        topMenuHost.Top += offset;
        _asd.SetBounds(_asd.Left, _asd.Top + offset, _asd.Width, _asd.Height - offset);

        IsVatSysMenuVisible = true;
        IsVatSysMenuVisibleChanged?.Invoke(this, EventArgs.Empty);
    }

    void HideVatSysMenu(ElementHost topMenuHost)
    {
        var offset = _vatSysMenu.Height;

        _vatSysMenu.Visible = false;
        topMenuHost.Top -= offset;
        _asd.SetBounds(_asd.Left, _asd.Top - offset, _asd.Width, _asd.Height + offset);

        IsVatSysMenuVisible = false;
        IsVatSysMenuVisibleChanged?.Invoke(this, EventArgs.Empty);
    }

    static int GetHeight(FrameworkElement view)
    {
        view.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        return (int)Math.Ceiling(view.DesiredSize.Height);
    }
}
