namespace Zebble
{
    using CoreGraphics;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using UIKit;

    public partial class BoxShadow
    {
        public Task<FileInfo> SaveAsPng(int imageWidth, int imageHeight, Color[] colors)
        {
            return Thread.UI.Run(async () =>
            {
                var image = DrawBitmap(colors, imageWidth, imageHeight);
                using (var imageData = image.AsPNG())
                {
                    var myByteArray = new byte[imageData.Length];
                    System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, myByteArray, 0,
                        Convert.ToInt32(imageData.Length));

                    await CurrentFile.WriteAllBytesAsync(myByteArray);

                    return CurrentFile;
                }
            });
        }

        UIImage DrawBitmap(Color[] color, int width, int height)
        {
            UIGraphics.BeginImageContextWithOptions(new CGSize(width, height), opaque: false, scale: 0.0f);
            var context = UIGraphics.GetCurrentContext();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var cgColor = color[index].Render().CGColor;
                    context.SetFillColor(cgColor);
                    context.FillRect(new CGRect(x, y, 1.0f, 1.0f));
                }
            }

            var image = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            return image;
        }
    }
}