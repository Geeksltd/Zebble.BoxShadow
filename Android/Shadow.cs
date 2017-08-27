using System;
using System.Threading.Tasks;
using Android.Graphics;
using System.IO;

namespace Zebble
{
    public partial class Shadow
    {
        public static Task SaveAsPng(FileInfo target, int imageWidth, int imageHeight, Color[] pixels)
        {
            if (pixels.Length != imageWidth * imageHeight)
                throw new Exception($"For a {imageWidth}X{imageHeight} image, an array of {imageWidth * imageHeight}" + " colors is expected.");

            // TODO: Create a bitmap image with the specified width and height.
            var bitmap = Bitmap.CreateBitmap(imageWidth, imageHeight, Bitmap.Config.Argb8888);

            // Then set each pixel from the array provided.
            for (int x = 0; x < imageWidth; x++)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    int z = x * y + y;
                    Android.Graphics.Color color = pixels[z].Render();
                    //  Android.Graphics.Color color = new Android.Graphics.Color(pixels[z].Red, pixels[z].Green, pixels[z].Blue, pixels[z].Alpha);
                    bitmap.SetPixel(x, y, color);
                }
            }

            // Then encode and save the bitmap as a PNG file.'
            using (var filestream = new FileStream(target.FullName, FileMode.OpenOrCreate))
            {
                if (bitmap.Compress(Bitmap.CompressFormat.Png, 100, filestream))
                {
                    filestream.Flush();
                }
                else { } // handle failure case...
            }
            bitmap.Recycle();
            bitmap.Dispose();

            return Task.CompletedTask;
        }

        // store Bitmap


        bool Save(FileInfo savePath, byte[] buffer)
        {
            // var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            // var path = System.IO.Path.Combine(sdCardPath, fileName);
            var bitmap = BitmapFactory.DecodeByteArray(buffer, 0, buffer.Length);

            using (var filestream = new FileStream(savePath.FullName, FileMode.OpenOrCreate))
            {
                if (bitmap.Compress(Bitmap.CompressFormat.Png, 50, filestream))
                {
                    filestream.Flush();
                }
                else { } // handle failure case...
            }
            bitmap.Recycle();
            bitmap.Dispose();
            return true;
        }
    }
}