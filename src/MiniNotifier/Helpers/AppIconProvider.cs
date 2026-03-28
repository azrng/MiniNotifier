using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MiniNotifier.Helpers;

public static class AppIconProvider
{
    private static readonly Uri IconResourceUri = new("pack://application:,,,/Assets/Icons/AppIcon.ico", UriKind.Absolute);

    public static Icon LoadTrayIcon()
    {
        try
        {
            var resourceInfo = Application.GetResourceStream(IconResourceUri);
            if (resourceInfo?.Stream is null)
            {
                return SystemIcons.Information;
            }

            using var iconStream = resourceInfo.Stream;
            using var icon = new Icon(iconStream);
            return (Icon)icon.Clone();
        }
        catch
        {
            return SystemIcons.Information;
        }
    }

    public static ImageSource LoadWindowIcon()
    {
        try
        {
            var image = BitmapFrame.Create(
                IconResourceUri,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad
            );
            image.Freeze();
            return image;
        }
        catch
        {
            return BitmapFrame.Create(
                new Uri("pack://application:,,,/Wpf.Ui;component/Resources/wpfui.ico", UriKind.Absolute)
            );
        }
    }
}
