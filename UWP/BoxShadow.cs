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

    public partial class BoxShadow
    {
        public async Task<FileInfo> SaveAsPng(int width, int height, Color[] colors)
        {
            return await Device.UIThread.Run(async () =>
            {
                var bitmap = new WriteableBitmap(width, height);

                var imageArray = colors.ToByteArray(width, height);

                using (Stream stream = bitmap.PixelBuffer.AsStream()) await stream.WriteAsync(imageArray, 0, imageArray.Length);

                var destFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(CurrentFile.Name);
                 
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

                await CurrentFile.WriteAllBytesAsync(await destFile.ReadAllBytes());
                return CurrentFile;
            });
        }
    }
}