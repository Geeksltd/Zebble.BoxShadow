namespace Zebble
{
    using Android.Graphics;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public partial class BoxShadow
    {
        public async Task<byte[]> SaveAsPng(int width, int height, Color[] colors)
        {
            byte[] result;
            using (var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888))
            {
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                        bitmap.SetPixel(x, y, colors[y * width + x].Render());

                using (var stream = new MemoryStream())
                {
                    bitmap.Compress(Bitmap.CompressFormat.Png, 0, stream);

                    using (var output = new MemoryStream())
                    {
                        bitmap.Compress(Bitmap.CompressFormat.Png, 0, output);
                        result = output.ReadAllBytes();
                    }

                    bitmap.Recycle();
                }
            }

            return result;
        }
    }
}