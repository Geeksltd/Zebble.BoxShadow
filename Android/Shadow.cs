using System;
using System.Threading.Tasks;
using Android.Graphics;
using System.IO;
using Android.Renderscripts;
using Zebble.Plugin;

namespace Zebble
{
    public partial class Shadow
    {
        public async static Task SaveAsPng(FileInfo target, int width, int height, int blurRadius, Color[] colors, int increaseValue)
        {
            //if (pixels.Length != imageWidth * imageHeight)
            //    throw new Exception($"For a {imageWidth}X{imageHeight} image, an array of {imageWidth * imageHeight}" + " colors is expected.");

            // TODO: Create a bitmap image with the specified width and height.
            var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
            //var bitsPerPixel = 4;


            //// Then set each pixel from the array provided.
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    // var index = i * bitsPerPixel;
                    //Android.Graphics.Color color = new Android.Graphics.Color(pixels[index], pixels[index + 1], pixels[index + 2], pixels[index + 3]);
                    bitmap.SetPixel(x, y, colors[i].Render());
                }

            MemoryStream stream = new MemoryStream();
            bitmap.Compress(Bitmap.CompressFormat.Png, 0, stream);

            // Then encode and save the bitmap as a PNG file.'
            using (var filestream = new FileStream(target.FullName, FileMode.OpenOrCreate))
            {
                if (bitmap.Compress(Bitmap.CompressFormat.Png, 0, filestream))
                {
                    filestream.Flush();
                }
                else { } // handle failure case...
            }
            // Then encode and save the bitmap as a PNG file.'

            bitmap.Recycle();
            bitmap.Dispose();

        }

        // store Bitmap



    }
}