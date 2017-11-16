namespace Zebble
{
    using System.IO;
    using System.Threading.Tasks;
    using UIKit;
    using CoreGraphics;
    using System;

    public partial class BoxShadow
    {
        public async Task<FileInfo> SaveAsPng(int imageWidth, int imageHeight, Color[] colors)
        {
            var byteArray = colors.ToByteArray(imageWidth, imageHeight);

            return await Device.UIThread.Run(async () =>
            {
                var image = ConvertBitmapRGBA8ToUIImage(byteArray, imageWidth, imageHeight);
                using (var imageData = image.AsPNG())
                {
                    var myByteArray = new byte[imageData.Length];
                    System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, myByteArray, 0,
                        Convert.ToInt32(imageData.Length));

                    await CurrentFile.WriteAllBytesAsync(myByteArray);

                    return CurrentFile;
                }
            });
        }

        UIImage ConvertBitmapRGBA8ToUIImage(byte[] buffer, int width, int height)
        {
            using (var provider = new CGDataProvider(buffer, 0, buffer.Length))
            {
                const int bitsPerComponent = 8;
                const int bitsPerPixel = 32;
                var bytesPerRow = 4 * width;

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