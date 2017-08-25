using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Windows.Graphics.DirectX.Direct3D11;
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
                throw new Exception($"For a {imageWidth}X{imageHeight} image, an array of {imageWidth * imageHeight}" +
                    " colors is expected.");

            // TODO: Create a bitmap image with the specified width and height.
            // Then set each pixel from the array provided.
            // Then encode and save the bitmap as a PNG file.
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

        //async Task Save(byte[] ImageArray)
        //{
        //    StorageFile sampleFile = await StorageFile.GetFileFromPathAsync("");
        //    await FileIO.WriteBytesAsync(sampleFile, ImageArray);
        //}

        //public async static Task<BitmapImage> ImageFromBytes2(Byte[] bytes)
        //{
        //    BitmapImage image = new BitmapImage();
        //    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
        //    {
        //        await stream.WriteAsync(bytes.AsBuffer());
        //        stream.Seek(0);
        //        await image.SetSourceAsync(stream);
        //    }
        //    return image;
        //}
        //public async Task<BitmapImage> ImageFromBytes(Byte[] bytes)
        //{
        //    BitmapDecoder decoder;
        //    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
        //    {
        //        await stream.WriteAsync(bytes.AsBuffer());
        //        stream.Seek(0);
        //        //  await image.SetSourceAsync(stream);
        //        decoder = await BitmapDecoder.CreateAsync(stream);
        //    }

        //    PixelDataProvider pixelData = await decoder.GetPixelDataAsync();
        //    var PixelArray = pixelData.DetachPixelData();
        //    WriteableBitmap bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
        //    await bitmap.PixelBuffer.AsStream().WriteAsync(PixelArray, 0, PixelArray.Length);

        //    InMemoryRandomAccessStream inMemoryRandomAccessStream = new InMemoryRandomAccessStream();
        //    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, inMemoryRandomAccessStream);
        //    Stream pixelStream = bitmap.PixelBuffer.AsStream();
        //    byte[] pixels = new byte[pixelStream.Length];
        //    await pixelStream.ReadAsync(pixels, 0, pixels.Length);
        //    encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96.0, 96.0, pixels);
        //    await encoder.FlushAsync();
        //    BitmapImage bitmapImage = new BitmapImage();
        //    bitmapImage.SetSource(inMemoryRandomAccessStream);

        //    return bitmapImage;
        //}

        //async Task save2()
        //{
        //    StorageFile savefile = await StorageFile.GetFileFromPathAsync("");
        //    IRandomAccessStream random = await RandomAccessStreamReference.CreateFromFile(savefile).OpenReadAsync();
        //    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(random);
        //    PixelDataProvider pixelData = await decoder.GetPixelDataAsync();
        //    var PixelArray = pixelData.DetachPixelData();
        //    WriteableBitmap bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
        //    await bitmap.PixelBuffer.AsStream().WriteAsync(PixelArray, 0, PixelArray.Length);
        //    //  MyImage.Source = bitmap;


        //    InMemoryRandomAccessStream inMemoryRandomAccessStream = new InMemoryRandomAccessStream();
        //    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, inMemoryRandomAccessStream);
        //    Stream pixelStream = bitmap.PixelBuffer.AsStream();
        //    byte[] pixels = new byte[pixelStream.Length];
        //    await pixelStream.ReadAsync(pixels, 0, pixels.Length);
        //    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96.0, 96.0, pixels);
        //    await encoder.FlushAsync();
        //    BitmapImage bitmapImage = new BitmapImage();
        //    bitmapImage.SetSource(inMemoryRandomAccessStream);

        //    //  MyImage.Source = bitmapImage;
        //}
        //async Task saveAsync()
        //{
        //    WriteableBitmap writeableBitmap = new WriteableBitmap(300, 300);
        //    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Logo.scale-100.png"));
        //    using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
        //    {
        //        // set the source for WriteableBitmap  
        //        await writeableBitmap.SetSourceAsync(fileStream);
        //    }
        //    // Save the writeableBitmap object to JPG Image file 


        //    StorageFile savefile = await StorageFile.GetFileFromPathAsync("");
        //    if (savefile == null)
        //        return;
        //    IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);
        //    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
        //    // Get pixels of the WriteableBitmap object 
        //    Stream pixelStream = writeableBitmap.PixelBuffer.AsStream();
        //    byte[] pixels = new byte[pixelStream.Length];
        //    await pixelStream.ReadAsync(pixels, 0, pixels.Length);
        //    // Save the image file with jpg extension 
        //    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)writeableBitmap.PixelWidth, (uint)writeableBitmap.PixelHeight, 96.0, 96.0, pixels);
        //    await encoder.FlushAsync();
        //}

        //private async void TranscodeImageFile(StorageFile imageFile)
        //{


        //    using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.ReadWrite))
        //    {
        //        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);

        //        var memStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        //        BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(memStream, decoder);

        //        encoder.BitmapTransform.ScaledWidth = 320;
        //        encoder.BitmapTransform.ScaledHeight = 240;

        //        await encoder.FlushAsync();

        //        memStream.Seek(0);
        //        fileStream.Seek(0);
        //        fileStream.Size = 0;
        //        await RandomAccessStream.CopyAsync(memStream, fileStream);

        //        memStream.Dispose();
        //    }
        //}


        //Task CreateImageFile1(FileInfo savePath)
        //{

        //using (Bitmap b = new Bitmap(50, 50))
        //{
        //    using (Graphics g = Graphics.FromImage(b))
        //    {
        //        g.Clear(Color.Green);
        //    }
        //    b.Save(@"C:\green.png", ImageFormat.Png);
        //}

        //  Image img = sender as Image;
        //System.Windows.Media.Effects.DropShadowEffect dse = new DropShadowEffect()
        //{
        //    Direction = 225,
        //    Color = Color.FromArgb(255, 182, 194, 203),
        //    ShadowDepth = 20,
        //    BlurRadius = 14
        //};
        //img.Effect = dse;
        //// Get modified image
        //RenderTargetBitmap rtb = new RenderTargetBitmap((int)img.Source.Width,
        //                                                 (int)img.Source.Height,
        //                                                 96d, 96d,
        //                                                 PixelFormats.Default);
        //DrawingVisual visual = new DrawingVisual();
        //using (DrawingContext ctx = visual.RenderOpen())
        //{
        //    VisualBrush vb = new VisualBrush(img);
        //    ctx.DrawRectangle(vb, null, new Rect(new Point(), new Point(img.Source.Width, img.Source.Height)));
        //}
        //rtb.Render(visual);

        //// Save modified image
        //BitmapEncoder encoder = new PngBitmapEncoder();
        //encoder.Frames.Add(BitmapFrame.Create(rtb));
        //using (Stream outputStream = File.OpenWrite(targetImageFile))
        //{
        //    encoder.Save(outputStream);
        //}


        //   return Task.CompletedTask;
        // TODO: Generate an image for the blur using semi transparent pixels:
        // }

    }
}
