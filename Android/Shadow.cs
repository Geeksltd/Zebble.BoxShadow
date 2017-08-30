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
        public static Task SaveAsPng(FileInfo target, int imageWidth, int imageHeight, int blurRadius, Color[] pixels)
        {
            if (pixels.Length != imageWidth * imageHeight)
                throw new Exception($"For a {imageWidth}X{imageHeight} image, an array of {imageWidth * imageHeight}" + " colors is expected.");

            // TODO: Create a bitmap image with the specified width and height.
            var bitmap = Bitmap.CreateBitmap(imageWidth, imageHeight, Bitmap.Config.Argb8888);
            //var bitsPerPixel = 4;

            //var resultArray = new byte[imageWidth * imageHeight * bitsPerPixel];
            //// Then set each pixel from the array provided.
            //for (int x = 0; x < imageWidth; x++)
            //{
            //    for (int y = 0; y < imageHeight; y++)
            //    {
            //        int z = x * y + y;
            //        Android.Graphics.Color color1 = pixels[z].Render();
            //        //  Android.Graphics.Color color = new Android.Graphics.Color(pixels[z].Red, pixels[z].Green, pixels[z].Blue, pixels[z].Alpha);
            //        bitmap.SetPixel(x, y, color1);
            //    }
            //}
            //MemoryStream stream = new MemoryStream();
            //bitmap.Compress(Bitmap.CompressFormat.Png, 0, stream);
            //byte[] imageArray = stream.ToArray();

            //if (blurRadius != 0)
            //{
            //    resultArray = GaussianBlur.Blur(imageArray, imageWidth, imageHeight, bitsPerPixel, blurRadius);
            //    imageArray = GaussianBlur.Blur(resultArray, imageWidth, imageHeight, bitsPerPixel, blurRadius);
            //    resultArray = GaussianBlur.Blur(imageArray, imageWidth, imageHeight, bitsPerPixel, blurRadius);
            //}


            var backgroundColor = Colors.White;
            var color = Colors.Black;
            var canvas = new Android.Graphics.Canvas(bitmap);

            // Background
            canvas.DrawARGB(backgroundColor.Alpha, backgroundColor.Red, backgroundColor.Green, backgroundColor.Blue);

            var paint = new Paint();
            // var color = info.Color.Render();
            paint.Color = color.Render();

            var rect = new Rect(blurRadius, blurRadius, imageWidth - blurRadius, imageHeight - blurRadius);
            canvas.DrawRect(rect, paint);

            var blurredBitmap = Bitmap.CreateBitmap(bitmap);
            var renderScript = RenderScript.Create(Zebble.Renderer.Context);

            // Allocate memory for Renderscript to work with
            var input = Allocation.CreateFromBitmap(renderScript, bitmap, Allocation.MipmapControl.MipmapFull, AllocationUsage.Script);
            var output = Allocation.CreateTyped(renderScript, input.Type);

            // Load up an instance of the specific script that we want to use.
            var script = ScriptIntrinsicBlur.Create(renderScript, Element.U8_4(renderScript));
            script.SetInput(input);

            // Set the blur radius
            script.SetRadius(blurRadius);

            // Start Renderscript working.
            script.ForEach(output);

            // Copy the output to the blurred bitmap
            output.CopyTo(blurredBitmap);

            // Then encode and save the bitmap as a PNG file.'
            using (var filestream = new FileStream(target.FullName, FileMode.OpenOrCreate))
            {
                if (blurredBitmap.Compress(Bitmap.CompressFormat.Png, 0, filestream))
                {
                    filestream.Flush();
                }
                else { } // handle failure case...
            }
            // Then encode and save the bitmap as a PNG file.'
            blurredBitmap.Recycle();
            blurredBitmap.Dispose();
            bitmap.Recycle();
            bitmap.Dispose();

            return Task.CompletedTask;
        }

        // store Bitmap



    }
}