namespace Zebble
{
    using System;
    using System.Threading.Tasks;

    internal static class GaussianBlur
    {
        const int WHITE = 255, BLACK = 0;

        static readonly ParallelOptions Poptions = new ParallelOptions { MaxDegreeOfParallelism = 16 };

        public static Color[] Blur(Color[] colors, int width, int height, int radius)
        {
            var bitsPerPixel = 4;
            var imageArray = colors.ToByteArray(width, height);

            var newRed = new byte[width * height];
            var newGreen = new byte[width * height];
            var newBlue = new byte[width * height];
            var newAlpha = new byte[width * height];
            var result = new Color[width * height];

            var arraySize = imageArray.Length / bitsPerPixel;
            var red = new byte[arraySize];
            var green = new byte[arraySize];
            var blue = new byte[arraySize];
            var alpha = new byte[arraySize];

            Parallel.For(0, imageArray.Length / bitsPerPixel, Poptions, i =>
            {
                var index = i * bitsPerPixel;

                red[i] = imageArray[index];
                green[i] = imageArray[index + 1];
                blue[i] = imageArray[index + 2];
                alpha[i] = imageArray[index + 3];
            });

            Parallel.Invoke(
                () => GaussBlur(alpha, newAlpha, width, height, radius),
                () => GaussBlur(red, newRed, width, height, radius),
                () => GaussBlur(green, newGreen, width, height, radius),
                () => GaussBlur(blue, newBlue, width, height, radius));

            Parallel.For(0, result.Length, Poptions, i =>
             {
                 if (newAlpha[i] > WHITE) newAlpha[i] = WHITE;

                 if (newRed[i] > WHITE) newRed[i] = WHITE;

                 if (newGreen[i] > WHITE) newGreen[i] = WHITE;

                 if (newBlue[i] > WHITE) newBlue[i] = WHITE;

                 if (newAlpha[i] < BLACK) newAlpha[i] = BLACK;

                 if (newRed[i] < BLACK) newRed[i] = BLACK;

                 if (newGreen[i] < BLACK) newGreen[i] = BLACK;

                 if (newBlue[i] < BLACK) newBlue[i] = BLACK;

                 result[i] = new Color(newRed[i], newGreen[i], newBlue[i], newAlpha[i]);
             });

            return result;
        }

        static int[] BoxesForGauss(double sigma, int number)  // standard deviation, number of boxes
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / number) + 1);  // Ideal averaging filter width 
            var wl = Math.Floor(wIdeal); if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (12 * sigma * sigma - number * wl * wl - 4 * number * wl - 3 * number) / (-4 * wl - 4);
            var roundedmIdeal = Math.Round(mIdeal);

            var sizes = new int[number];
            for (var i = 0; i < number; i++)
                sizes[i] = (int)(i < roundedmIdeal ? wl : wu);

            return sizes;
        }

        static void GaussBlur(byte[] scl, byte[] tcl, int width, int height, double radius)
        {
            var bxs = BoxesForGauss(radius, 3);
            BoxBlur(scl, tcl, width, height, (bxs[0] - 1) / 2);
            BoxBlur(tcl, scl, width, height, (bxs[1] - 1) / 2);
            BoxBlur(scl, tcl, width, height, (bxs[2] - 1) / 2);
        }

        static void BoxBlur(byte[] scl, byte[] tcl, int width, int height, double radius)
        {
            for (var i = 0; i < scl.Length; i++) tcl[i] = scl[i];
            BoxBlurH(tcl, scl, width, height, radius);
            BoxBlurT(scl, tcl, width, height, radius);
        }

        static void BoxBlurH(byte[] scl, byte[] tcl, int width, int height, double radius)
        {
            var iarr = 1 / (radius + radius + 1);
            for (var i = 0; i < height; i++)
            {
                var ti = i * width;
                var li = ti;
                var ri = ti + (int)radius;
                var fv = scl[ti];
                var lv = scl[ti + width - 1];
                var val = (radius + 1) * fv;
                for (var j = 0; j < radius; j++)
                    val += scl[ti + j];
                for (var j = 0; j <= radius; j++)
                {
                    val += scl[ri++] - fv;
                    tcl[ti++] = Convert.ToByte(Math.Round(val * iarr));
                }

                for (var j = radius + 1; j < width - radius; j++)
                {
                    val += scl[ri++] - scl[li++];
                    tcl[ti++] = Convert.ToByte(Math.Round(val * iarr));
                }

                for (var j = width - radius; j < width; j++)
                {
                    val += lv - scl[li++];
                    tcl[ti++] = Convert.ToByte(Math.Round(val * iarr));
                }
            }
        }

        static void BoxBlurT(byte[] scl, byte[] tcl, int width, int height, double radius)
        {
            var iarr = 1 / (radius + radius + 1);
            for (var i = 0; i < width; i++)
            {
                var ti = i;
                var li = ti;
                var ri = ti + (int)radius * width;
                var fv = scl[ti];
                var lv = scl[ti + width * (height - 1)];
                var val = (radius + 1) * fv;
                for (var j = 0; j < radius; j++) val += scl[ti + j * width];
                for (var j = 0; j <= radius; j++)
                {
                    val += scl[ri] - fv;
                    tcl[ti] = Convert.ToByte(Math.Round(val * iarr));
                    ri += width; ti += width;
                }

                for (var j = radius + 1; j < height - radius; j++)
                {
                    val += scl[ri] - scl[li];
                    tcl[ti] = Convert.ToByte(Math.Round(val * iarr));
                    li += width; ri += width; ti += width;
                }

                for (var j = height - radius; j < height; j++)
                {
                    val += lv - scl[li];
                    tcl[ti] = Convert.ToByte(Math.Round(val * iarr));
                    li += width; ti += width;
                }
            }
        }
    }
}