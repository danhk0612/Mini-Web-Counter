$ErrorActionPreference = 'Stop'

$path = 'MainForm.cs'
$text = Get-Content $path -Raw -Encoding UTF8

function Replace-Required([string]$old, [string]$new) {
    if (-not $script:text.Contains($old)) { throw 'Required MainForm block not found.' }
    $script:text = $script:text.Replace($old, $new)
}

Replace-Required @'
public sealed class MainForm : Form
{
    private const int BaseCardWidth = 180;
'@ @'
public sealed class MainForm : Form
{
    private const int WmMouseActivate = 0x0021;
    private const int MaNoActivate = 3;
    private const string NoActivateActionTag = "NoActivateAction";

    private const int BaseCardWidth = 180;
'@

Replace-Required @'
    private static Panel CreateIconPanel(int x, int y, int size)
    {
        return new Panel
        {
            Location = new Point(x, y),
            Size = new Size(size, size),
            BackColor = Color.Transparent,
            TabStop = false
        };
    }

    private static void DrawLinkIcon
'@ @'
    private static Panel CreateIconPanel(int x, int y, int size)
    {
        return new Panel
        {
            Location = new Point(x, y),
            Size = new Size(size, size),
            BackColor = Color.Transparent,
            TabStop = false,
            Tag = NoActivateActionTag
        };
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmMouseActivate && IsNoActivateActionUnderCursor())
        {
            m.Result = (IntPtr)MaNoActivate;
            return;
        }

        base.WndProc(ref m);
    }

    private bool IsNoActivateActionUnderCursor()
    {
        var screenPoint = Cursor.Position;
        var windowHandle = WindowFromPoint(screenPoint);
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        var control = Control.FromHandle(windowHandle);
        while (control is not null && control != this)
        {
            if (string.Equals(control.Tag as string, NoActivateActionTag, StringComparison.Ordinal))
            {
                return true;
            }

            control = control.Parent;
        }

        return false;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    private static void DrawLinkIcon
'@

[System.IO.File]::WriteAllText((Resolve-Path $path), $text, [System.Text.UTF8Encoding]::new($false))
