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
        public async static Task SaveAsPng(FileInfo target, int width, int height, Color[] colors)
        {
            //if (pixels.Length != imageWidth * imageHeight)
            //    throw new Exception($"For a {imageWidth}X{imageHeight} image, an array of {imageWidth * imageHeight}" + " colors is expected.");
            await Device.UIThread.Run(async () =>
            {
                // TODO: Create a bitmap image with the specified width and height.
                WriteableBitmap bitmap = new WriteableBitmap(width, height);

                const int bitsPerPixel = 4;
                var imageArray = new byte[width * height * bitsPerPixel];

                for (int i = 0; i < imageArray.Length; i += 4)
                {
                    var pixelNumber = i / bitsPerPixel;
                    var color = colors[pixelNumber].Render();

                    imageArray[i] = color.B; // Blue
                    imageArray[i + 1] = color.G;  // Green
                    imageArray[i + 2] = color.R; // Red
                    imageArray[i + 3] = color.A;  // Alpha                
                }

                // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer 
                using (Stream stream = bitmap.PixelBuffer.AsStream())
                {
                    await stream.WriteAsync(imageArray, 0, imageArray.Length);
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
        }
    }
}
