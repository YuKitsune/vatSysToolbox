using System.Windows.Forms;
using System.Windows.Forms.Integration;
using EurocatMenusPlugin.Views;

namespace EurocatMenusPlugin;

/// <summary>
/// Hides the vatSys menu bar, and puts the top menu in its place. Puts the bottom menu below the ASD.
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

    public MenuBarLayout(Form mainForm)
    {
        _vatSysMenu = mainForm.MainMenuStrip
            ?? throw new InvalidOperationException("Cannot find the vatSys menu bar");

        _asd = mainForm.Controls[AsdControlName]
            ?? throw new InvalidOperationException("Cannot find the ASD");

        // The ASD starts below the vatSys menu bar. The top menu uses the same space.
        _topMenuHost = new ElementHost
        {
            Child = new TopMenu(),
            Location = new System.Drawing.Point(0, 0),
            Size = new System.Drawing.Size(mainForm.ClientSize.Width, _vatSysMenu.Height),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        mainForm.Controls.Add(_topMenuHost);

        // The bottom menu uses the bottom of the ASD. It has the same height as the top menu.
        var bottomMenuHeight = _vatSysMenu.Height;
        var bottomMenuHost = new ElementHost
        {
            Child = new BottomMenu(),
            Location = new System.Drawing.Point(0, _asd.Bottom - bottomMenuHeight),
            Size = new System.Drawing.Size(mainForm.ClientSize.Width, bottomMenuHeight),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        mainForm.Controls.Add(bottomMenuHost);
        _asd.Height -= bottomMenuHeight;

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
}
