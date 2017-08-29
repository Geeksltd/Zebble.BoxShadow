using System;
using System.IO;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using CoreGraphics;
using UIKit;
using System.Drawing;
using Foundation;
using System.Runtime.InteropServices;

namespace Zebble
{
    public partial class Shadow
    {
        public static Task SaveAsPng(FileInfo target, int imageWidth, int imageHeight, int blurRadius, Color[] pixels)
        {
            Color backgroundColor = Colors.White;
            Color color = Colors.Black;
            var rect = new CGRect(0, 0, imageWidth, imageHeight);


            //var blur = UIBlurEffect.FromStyle(UIBlurEffectStyle.Light);
            //var blurView = new UIVisualEffectView(blur)
            //{
            //    Frame = new RectangleF(0, 0, imageWidth, imageHeight)
            //};

            UIImage sourceImage = new UIImage();
            sourceImage.Draw(rect);

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


            //var pixelsWide = imageWidth;
            //var pixelsHigh = imageHeight;
            //var bitmapBytesPerRow = pixelsWide * 4;
            //var bitmapByteCount = bitmapBytesPerRow * pixelsHigh;
            ////Note implicit colorSpace.Dispose() 
            //using (var colorSpace = CGColorSpace.CreateDeviceRGB())
            //{
            //    //Allocate the bitmap and create context
            //    var bitmapData = Marshal.AllocHGlobal(bitmapByteCount);


            //    var context = new CGBitmapContext(bitmapData, pixelsWide, pixelsHigh, 8,
            //                                      bitmapBytesPerRow, colorSpace, CGImageAlphaInfo.PremultipliedFirst);             
            //}


            return Task.CompletedTask;
        }

    }
}