namespace Zebble
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Graphics.Imaging;
    using Windows.UI.Xaml.Media.Imaging;
    using Windows.Storage.Streams;
    using Windows.Storage;
    using System.Runtime.InteropServices.WindowsRuntime;

    public partial class Shadow
    {
        public async Task SaveAsPng(FileInfo target, int width, int height, Color[] colors)
        {
            await Device.UIThread.Run(async () =>
            {
                var bitmap = new WriteableBitmap(width, height);

                const int bitsPerPixel = 4;
                var imageArray = new byte[width * height * bitsPerPixel];

                for (int i = 0; i < imageArray.Length; i += 4)
                {
                    var pixelNumber = i / bitsPerPixel;
                    var color = colors[pixelNumber].Render();

                    imageArray[i] = color.R;
                    imageArray[i + 1] = color.G;
                    imageArray[i + 2] = color.B;
                    imageArray[i + 3] = color.A;
                }

                using (Stream stream = bitmap.PixelBuffer.AsStream()) await stream.WriteAsync(imageArray, 0, imageArray.Length);

                var destFile = await target.ToStorageFile();
                using (IRandomAccessStream stream = await destFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    var pixelStream = bitmap.PixelBuffer.AsStream();
                    var pixelsArray = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixelsArray, 0, pixelsArray.Length);

                    encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight,
                                (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96.0, 96.0, pixelsArray);
                    await encoder.FlushAsync();
                }
            });
        }
    }
}