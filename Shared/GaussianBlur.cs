using System;
using System.Collections.Generic;
using System.Text;

namespace Zebble.Plugin
{
    internal static class GaussianBlur
    {
        public static byte[] Blur(byte[] image, int width, int height, int bitsPerPixel, int radial)
        {
            var newRed = new byte[width * height];
            var newGreen = new byte[width * height];
            var newBlue = new byte[width * height];
            var result = new byte[width * height * bitsPerPixel];

            var red = new byte[image.Length / bitsPerPixel];
            var green = new byte[image.Length / bitsPerPixel];
            var blue = new byte[image.Length / bitsPerPixel];

            for (var i = 0; i < image.Length / bitsPerPixel; i++)
            {
                var index = i * bitsPerPixel;

                // skipping alpha
                blue[i] = image[index];
                green[i] = image[index + 1];
                red[i] = image[index + 2];
            }
            // GaussBlur2(image, result, radial, width, height);
            GaussBlur(red, newRed, radial, width, height);
            GaussBlur(green, newGreen, radial, width, height);
            GaussBlur(blue, newBlue, radial, width, height);

            for (var i = 0; i < result.Length / bitsPerPixel; i++)
            {
                var index = i * bitsPerPixel;

                result[index] = newBlue[i];
                result[index + 1] = newGreen[i];
                result[index + 2] = newRed[i];
                result[index + 3] = image[index + 3];
            }

            return result;
        }

        static void GaussBlur(byte[] source, byte[] target, int radius, int width, int height)
        {
            var rs = (int)Math.Ceiling(radius * 2.57); // significant radius

            var twoR2 = 2 * radius * radius;
            var piTwoR2 = Math.PI * twoR2;
            var maxX = width - 1;

            for (var i = 0; i < height; i++)
            {
                var minY = i - rs;
                var maxY = i + rs + 1;

                for (var j = 0; j < width; j++)
                {
                    var maxJX = j + rs + 1;
                    var minX = j - rs;
                    var val = 0D;
                    var wsum = 0D;

                    for (int iy = minY; iy < maxY; iy++)
                    {
                        var currentY = Math.Min(height - 1, Math.Max(0, iy));
                        var yWidth = currentY * width;
                        var dsqy = (iy - i) * (iy - i);

                        for (int ix = minX; ix < maxJX; ix++)
                        {
                            var currentX = ix.LimitWithin(0, maxX);
                            var dsq = (ix - j) * (ix - j) + dsqy;
                            var wght = Math.Exp(-dsq / twoR2) / piTwoR2;
                            val += source[yWidth + currentX] * wght;
                            wsum += wght;
                        }
                    }
                    target[i * width + j] = (byte)Math.Round(val / wsum);
                }
            }
        }

    }
}
