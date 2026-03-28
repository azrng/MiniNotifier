using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MiniNotifier.Helpers;

public static class AppIconProvider
{
    private static string IconPath =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "AppIcon.ico");

    public static Icon LoadTrayIcon()
    {
        return File.Exists(IconPath) ? new Icon(IconPath) : SystemIcons.Information;
    }

    public static ImageSource LoadWindowIcon()
    {
        if (!File.Exists(IconPath))
        {
            return BitmapFrame.Create(
                new Uri("pack://application:,,,/Wpf.Ui;component/Resources/wpfui.ico", UriKind.Absolute)
            );
        }

        var image = BitmapFrame.Create(
            new Uri(IconPath, UriKind.Absolute),
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad
        );
        image.Freeze();
        return image;
    }
}
