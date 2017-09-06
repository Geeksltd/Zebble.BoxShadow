using System.IO;
using System.Threading.Tasks;
using CoreGraphics;
using UIKit;
using Foundation;
using System;

namespace Zebble
{
    public partial class Shadow
    {
        public static async Task SaveAsPng(FileInfo target, int imageWidth, int imageHeight, Color[] colors)
        {
            var byteArray = new byte[imageWidth * imageHeight * 4];

            for (int i = 0; i < colors.Length; i += 4)
            {
                var pixelNumber = i / 4;
                var color = colors[pixelNumber];

                byteArray[i] = color.Blue; // Blue
                byteArray[i + 1] = color.Green;  // Green
                byteArray[i + 2] = color.Red; // Red
                byteArray[i + 3] = color.Alpha;  // Alpha                
            }

            // colors.CopyTo(byteArray, 0);

            string base64String = Convert.ToBase64String(byteArray);

            NSData nSData = new NSData(base64String, NSDataBase64DecodingOptions.IgnoreUnknownCharacters);

            UIImage sourceImage = new UIImage(nSData);

            using (var ns = new NSAutoreleasePool())
            {
                NSError err;
                using (UIImage img = sourceImage)
                {
                    using (var data = img.AsPNG())
                    {
                        data.Save(target.FullName, true, out err);
                    }
                }
            }
        }
    }
}