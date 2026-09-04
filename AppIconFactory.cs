using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace JCMS_Mini_Monitoring;

internal static class AppIconFactory
{
    public static Icon Create(int size = 64)
    {
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var scale = size / 64F;
        using var backgroundPath = CreateRoundedRectangle(
            new RectangleF(2F * scale, 2F * scale, 60F * scale, 60F * scale),
            13F * scale);
        using var backgroundBrush = new SolidBrush(Color.FromArgb(39, 52, 73));
        graphics.FillPath(backgroundBrush, backgroundPath);

        var bars = new[]
        {
            (X: 12F, Y: 31F, H: 20F, Color: Color.FromArgb(220, 64, 64)),
            (X: 24F, Y: 23F, H: 28F, Color: Color.FromArgb(62, 164, 102)),
            (X: 36F, Y: 15F, H: 36F, Color: Color.FromArgb(235, 159, 55)),
            (X: 48F, Y: 27F, H: 24F, Color: Color.FromArgb(64, 128, 210))
        };

        foreach (var bar in bars)
        {
            var rect = new RectangleF(
                bar.X * scale,
                bar.Y * scale,
                7F * scale,
                bar.H * scale);
            using var path = CreateRoundedRectangle(rect, 3F * scale);
            using var brush = new SolidBrush(bar.Color);
            graphics.FillPath(brush, path);
        }

        var iconHandle = bitmap.GetHicon();
        try
        {
            using var temporaryIcon = Icon.FromHandle(iconHandle);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
    {
        var diameter = radius * 2F;
        var path = new GraphicsPath();

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
