using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using MediaImageSource = System.Windows.Media.ImageSource;

namespace MiniNotifier.Helpers;

public static class AppIconFactory
{
    public static Icon CreateTrayIcon(int size = 64)
    {
        using var bitmap = CreateBitmap(size);
        var handle = bitmap.GetHicon();

        try
        {
            using var sourceIcon = Icon.FromHandle(handle);
            return (Icon)sourceIcon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    public static MediaImageSource CreateWindowIcon(int size = 96)
    {
        using var bitmap = CreateBitmap(size);
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);
        memoryStream.Position = 0;

        var image = BitmapFrame.Create(memoryStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        image.Freeze();
        return image;
    }

    private static Bitmap CreateBitmap(int size)
    {
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.Clear(System.Drawing.Color.Transparent);

        var rect = new RectangleF(4, 4, size - 8, size - 8);
        using var shadowBrush = new SolidBrush(System.Drawing.Color.FromArgb(36, 111, 195, 255));
        graphics.FillEllipse(shadowBrush, new RectangleF(size * 0.42f, size * 0.42f, size * 0.28f, size * 0.28f));

        using var backgroundPath = CreateRoundedRect(rect, size * 0.22f);
        using var backgroundBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new PointF(rect.Left, rect.Top),
            new PointF(rect.Right, rect.Bottom),
            System.Drawing.Color.FromArgb(76, 132, 240),
            System.Drawing.Color.FromArgb(30, 58, 138)
        );
        graphics.FillPath(backgroundBrush, backgroundPath);

        using var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(76, 255, 255, 255), Math.Max(1f, size * 0.03f));
        graphics.DrawPath(borderPen, backgroundPath);

        using var glowBrush = new SolidBrush(System.Drawing.Color.FromArgb(38, 255, 255, 255));
        graphics.FillEllipse(glowBrush, new RectangleF(size * 0.18f, size * 0.16f, size * 0.26f, size * 0.26f));

        using var dropletPath = new GraphicsPath();
        dropletPath.AddBezier(
            size * 0.5f,
            size * 0.2f,
            size * 0.64f,
            size * 0.35f,
            size * 0.76f,
            size * 0.48f,
            size * 0.76f,
            size * 0.61f
        );
        dropletPath.AddBezier(
            size * 0.76f,
            size * 0.76f,
            size * 0.62f,
            size * 0.86f,
            size * 0.5f,
            size * 0.86f,
            size * 0.38f,
            size * 0.86f
        );
        dropletPath.AddBezier(
            size * 0.24f,
            size * 0.76f,
            size * 0.24f,
            size * 0.48f,
            size * 0.36f,
            size * 0.35f,
            size * 0.5f,
            size * 0.2f
        );

        using var dropletBrush = new SolidBrush(System.Drawing.Color.FromArgb(250, 248, 250, 252));
        graphics.FillPath(dropletBrush, dropletPath);

        using var highlightPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(168, 233, 249, 255), Math.Max(1.5f, size * 0.03f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawArc(
            highlightPen,
            size * 0.41f,
            size * 0.34f,
            size * 0.15f,
            size * 0.2f,
            180,
            80
        );

        return bitmap;
    }

    private static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();

        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
