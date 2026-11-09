using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace EurocatMenusPlugin;

/// <summary>
/// Hides the vatSys menu bar. Puts the top menu at the top of the main form, and the bottom menu at the bottom.
/// The views set the height of the menus. The ASD fills the space between the menus.
/// The vatSys menu bar can show again above the top menu. The ASD then moves down to make space for it.
/// </summary>
public class MenuBarLayout
{
    // The name of the ASD control on the vatSys main form
    const string AsdControlName = "ASD";

    readonly MenuStrip _vatSysMenu;
    readonly Control _asd;
    readonly ElementHost _topMenuHost;

    bool _vatSysMenuVisible;

    public MenuBarLayout(Form mainForm, FrameworkElement topMenu, FrameworkElement bottomMenu)
    {
        _vatSysMenu = mainForm.MainMenuStrip
            ?? throw new InvalidOperationException("Cannot find the vatSys menu bar");

        _asd = mainForm.Controls[AsdControlName]
            ?? throw new InvalidOperationException("Cannot find the ASD");

        var topMenuHeight = GetHeight(topMenu);
        _topMenuHost = new ElementHost
        {
            Child = topMenu,
            Location = new System.Drawing.Point(0, 0),
            Size = new System.Drawing.Size(mainForm.ClientSize.Width, topMenuHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        mainForm.Controls.Add(_topMenuHost);

        var bottomMenuHeight = GetHeight(bottomMenu);
        var bottomMenuHost = new ElementHost
        {
            Child = bottomMenu,
            Location = new System.Drawing.Point(0, _asd.Bottom - bottomMenuHeight),
            Size = new System.Drawing.Size(mainForm.ClientSize.Width, bottomMenuHeight),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        mainForm.Controls.Add(bottomMenuHost);

        _asd.SetBounds(_asd.Left, topMenuHeight, _asd.Width, bottomMenuHost.Top - topMenuHeight);

        _vatSysMenu.Visible = false;
        _vatSysMenuVisible = false;
    }

    public void ToggleVatSysMenu()
    {
        if (_vatSysMenuVisible)
            HideVatSysMenu();
        else
            ShowVatSysMenu();
    }

    void ShowVatSysMenu()
    {
        var offset = _vatSysMenu.Height;

        _vatSysMenu.Visible = true;
        _topMenuHost.Top += offset;
        _asd.SetBounds(_asd.Left, _asd.Top + offset, _asd.Width, _asd.Height - offset);

        _vatSysMenuVisible = true;
    }

    void HideVatSysMenu()
    {
        var offset = _vatSysMenu.Height;

        _vatSysMenu.Visible = false;
        _topMenuHost.Top -= offset;
        _asd.SetBounds(_asd.Left, _asd.Top - offset, _asd.Width, _asd.Height + offset);

        _vatSysMenuVisible = false;
    }

    static int GetHeight(FrameworkElement view)
    {
        view.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        return (int)Math.Ceiling(view.DesiredSize.Height);
    }
}
