using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;


namespace Zebble
{
    public partial class Shadow
    {
        public static Task SaveAsPng(FileInfo target, int imageWidth, int imageHeight, Color[] pixels)
        {
            if (pixels.Length != imageWidth * imageHeight)
                throw new Exception($"For a {imageWidth}X{imageHeight} image, an array of {imageWidth * imageHeight}" + " colors is expected.");
            Device.UIThread.Run(async () =>
            {
                // TODO: Create a bitmap image with the specified width and height.
                WriteableBitmap bitmap = new WriteableBitmap(imageWidth, imageHeight);

                // Then set each pixel from the array provided.
                // WriteableBitmap uses BGRA format which is 4 bytes per pixel.
                byte[] imageArray = new byte[imageHeight * imageWidth * 4];
                for (int i = 0; i < imageArray.Length; i += 4)
                {
                    var color = pixels[i].Render();
                    imageArray[i] = color.B; // Blue
                    imageArray[i + 1] = color.G;  // Green
                    imageArray[i + 2] = color.R; // Red
                    imageArray[i + 3] = color.A;  // Alpha
                }
                // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer 
                using (Stream stream = bitmap.PixelBuffer.AsStream())
                {
                    stream.WriteAsync(imageArray, 0, imageArray.Length);
                }
                // Then encode and save the bitmap as a PNG file.

                target.Create().Dispose();
                StorageFile destFile = await target.ToStorageFile();
                using (IRandomAccessStream stream = await destFile.OpenAsync(FileAccessMode.ReadWrite))     //   using (IRandomAccessStream stream = target.Create().AsRandomAccessStream())
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    Stream pixelStream = bitmap.PixelBuffer.AsStream();
                    byte[] pixelsArray = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixelsArray, 0, pixels.Length);

                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96.0, 96.0, pixelsArray);
                    await encoder.FlushAsync();
                }
            });
            return Task.CompletedTask;
        }



        bool Save(FileInfo savePath, byte[] buffer)
        {
            Device.UIThread.Run(async () =>
            {
                WriteableBitmap wb = new WriteableBitmap(50, 50);
                using (Stream stream = wb.PixelBuffer.AsStream())
                {
                    if (stream.CanWrite)
                    {
                        stream.WriteAsync(buffer, 0, buffer.Length);
                        stream.Flush();
                    }
                }

                StorageFile destFile = await KnownFolders.PicturesLibrary.CreateFileAsync(savePath.FullName, CreationCollisionOption.ReplaceExisting);
                using (IRandomAccessStream stream = await destFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    Stream pixelStream = wb.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                (uint)wb.PixelWidth, (uint)wb.PixelHeight, 96.0, 96.0, pixels);
                    await encoder.FlushAsync();
                }
            });
            return true;
        }
    }
}
