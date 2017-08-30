using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using Zebble.Plugin;

namespace Zebble
{
    public partial class Shadow
    {
        public static Task SaveAsPng(FileInfo target, int imageWidth, int imageHeight, int blurRadius, Color[] pixels)
        {
            if (pixels.Length != imageWidth * imageHeight)
                throw new Exception($"For a {imageWidth}X{imageHeight} image, an array of {imageWidth * imageHeight}" + " colors is expected.");
            Device.UIThread.Run(async () =>
            {
                // TODO: Create a bitmap image with the specified width and height.
                WriteableBitmap bitmap = new WriteableBitmap(imageWidth, imageHeight);

                // Then set each pixel from the array provided.
                // WriteableBitmap uses BGRA format which is 4 bytes per pixel.

                const int bitsPerPixel = 4;
                var imageArray = new byte[imageWidth * imageHeight * bitsPerPixel];
                var resultArray = new byte[imageWidth * imageHeight * bitsPerPixel];
                for (int i = 0; i < imageArray.Length; i += 4)
                {
                    var pixelNumber = i / bitsPerPixel;
                    var color = pixels[pixelNumber].Render();

                    imageArray[i] = color.B; // Blue
                    imageArray[i + 1] = color.G;  // Green
                    imageArray[i + 2] = color.R; // Red
                    imageArray[i + 3] = color.A;  // Alpha                
                }

                // Blur it
                if (blurRadius != 0)
                {
                    resultArray = GaussianBlur.Blur(imageArray, imageWidth, imageHeight, bitsPerPixel, blurRadius);
                    imageArray = GaussianBlur.Blur(resultArray, imageWidth, imageHeight, bitsPerPixel, blurRadius);
                    resultArray = GaussianBlur.Blur(imageArray, imageWidth, imageHeight, bitsPerPixel, blurRadius);
                }
                // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer 
                using (Stream stream = bitmap.PixelBuffer.AsStream())
                {
                    await stream.WriteAsync(resultArray, 0, resultArray.Length);
                }
                // Then encode and save the bitmap as a PNG file.

                target.Create().Dispose();
                StorageFile destFile = await target.ToStorageFile();
                using (IRandomAccessStream stream = await destFile.OpenAsync(FileAccessMode.ReadWrite))     //   using (IRandomAccessStream stream = target.Create().AsRandomAccessStream())
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    Stream pixelStream = bitmap.PixelBuffer.AsStream();
                    byte[] pixelsArray = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixelsArray, 0, pixelsArray.Length);

                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                                (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96.0, 96.0, pixelsArray);
                    await encoder.FlushAsync();
                }
            });
            return Task.CompletedTask;
        }
    }
}
