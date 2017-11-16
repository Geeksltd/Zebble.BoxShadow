namespace Zebble
{
    using System.IO;
    using System.Threading.Tasks;
    using UIKit;
    using Foundation;
    using System;
    using CoreGraphics;
    using System.Linq;

    public partial class Shadow
    {
        public async Task SaveAsPng(FileInfo target, int imageWidth, int imageHeight, Color[] colors)
        {
            var byteArray = new byte[imageWidth * imageHeight * 4];

            for (var i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                var bytePosition = i * 4;

                byteArray[bytePosition] = color.Red;
                byteArray[bytePosition + 1] = color.Green;
                byteArray[bytePosition + 2] = color.Blue;
                byteArray[bytePosition + 3] = color.Alpha;
            }

            await Device.UIThread.Run(() =>
            {
                NSError err = null;
                var image = ConvertBitmapRGBA8ToUIImage(byteArray, imageWidth, imageHeight);
                var imageData = image.AsPNG();
                if (imageData == null || !imageData.Save(target.FullName, auxiliaryFile: true, error: out err))
                {
                    Device.Log.Error("file not saved as " + target.FullName);
                    Device.Log.Error(err);
                }
            });
        }

        public static UIImage ConvertBitmapRGBA8ToUIImage(byte[] buffer, int width, int height)
        {
            using (var provider = new CGDataProvider(buffer, 0, buffer.Length))
            {
                int bitsPerComponent = 8;
                int bitsPerPixel = 32;
                int bytesPerRow = 4 * width;

                var colorSpaceRef = CGColorSpace.CreateDeviceRGB();

                if (colorSpaceRef == null)
                {
                    Device.Log.Error(@"Error allocating color space");
                    return null;
                }

                var bitmapInfo = CGBitmapFlags.ByteOrderDefault;
                var renderingIntent = CGColorRenderingIntent.Default;

                var iref = new CGImage(width, height, bitsPerComponent, bitsPerPixel, bytesPerRow, colorSpaceRef, bitmapInfo, provider, null, true, renderingIntent);

                var pixels = new byte[buffer.Length];
                var context = new CGBitmapContext(pixels, width, height, bitsPerComponent, bytesPerRow, colorSpaceRef, CGImageAlphaInfo.PremultipliedLast);

                if (context == null)
                {
                    Device.Log.Error(@"Error context not created");
                    return null;
                }

                UIImage image = null;

                context.DrawImage(new CGRect(0.0f, 0.0f, width, height), iref);
                var imageRef = context.ToImage();

                var scale = UIScreen.MainScreen.Scale;
                image = new UIImage(imageRef, scale: scale, orientation: UIImageOrientation.Up);

                imageRef.Dispose();
                context.Dispose();
                colorSpaceRef.Dispose();
                iref.Dispose();
                pixels = null;

                return image;
            }
        }
    }
}