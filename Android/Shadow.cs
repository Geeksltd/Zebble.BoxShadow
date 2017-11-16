namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Android.Graphics;
    using System.IO;

    public partial class Shadow
    {
        public async Task SaveAsPng(FileInfo target, int width, int height, Color[] colors)
        {
            var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
            
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    bitmap.SetPixel(x, y, colors[index].Render());
                }

            var stream = new MemoryStream();
            bitmap.Compress(Bitmap.CompressFormat.Png, 0, stream);

            using (var filestream = new FileStream(target.FullName, FileMode.OpenOrCreate))
            {
                if (bitmap.Compress(Bitmap.CompressFormat.Png, 0, filestream))
                {
                    filestream.Flush();
                }
                else { }
            }

            bitmap.Recycle();
            bitmap.Dispose();
        }
    }
}