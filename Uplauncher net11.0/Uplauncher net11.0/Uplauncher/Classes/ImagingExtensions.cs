using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Uplauncher.Classes
{
    internal static class ImagingExtensions
    {
        public static void FromBitmapResource(this Image image, Type callingType, string relativePath)
        {
            string assemblyName = callingType.Assembly.FullName;
            BitmapImage bi = new BitmapImage(new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyName, relativePath), UriKind.RelativeOrAbsolute));
            image.Source = bi;
        }

        public static Image CaptureImage(this FrameworkElement me, bool ensureSize = false)
        {
            int width = Convert.ToInt32(me.ActualWidth);
            width = ((width == 0) ? 1 : width);
            int height = Convert.ToInt32(me.ActualHeight);
            height = ((height == 0) ? 1 : height);
            RenderTargetBitmap bmp = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Default);
            bmp.Render(me);
            return new Image
            {
                Source = bmp,
                Stretch = Stretch.None,
                Width = (double)(width - (ensureSize ? 1 : 0)),
                Height = (double)(height - (ensureSize ? 1 : 0))
            };
        }
    }
}
