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
            var bxs = BoxesForGauss(radial, 3);
            boxBlur_4(red, newRed, width, height, (bxs[0] - 1) / 2);
            boxBlur_4(green, newGreen, width, height, (bxs[1] - 1) / 2);
            boxBlur_4(blue, newBlue, width, height, (bxs[2] - 1) / 2);

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

        static void boxBlur_4(byte[] scl, byte[] tcl, int w, int h, double r)
        {
            for (var i = 0; i < scl.Length; i++) tcl[i] = scl[i];
            boxBlurH_4(tcl, scl, w, h, r);
            boxBlurT_4(scl, tcl, w, h, r);
        }
        static void boxBlurH_4(byte[] scl, byte[] tcl, int w, int h, double r)
        {
            double iarr = 1 / (r + r + 1);
            for (var i = 0; i < h; i++)
            {
                var ti = i * w;
                var li = ti;
                int ri = Convert.ToInt32(ti + r);
                var fv = scl[ti];
                var lv = scl[ti + w - 1];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += scl[ti + j];
                for (var j = 0; j <= r; j++) { val += scl[ri++] - fv; tcl[ti++] = Convert.ToByte(Math.Round(val * iarr)); }
                for (var j = r + 1; j < w - r; j++) { val += scl[ri++] - scl[li++]; tcl[ti++] = Convert.ToByte(Math.Round(val * iarr)); }
                for (var j = w - r; j < w; j++) { val += lv - scl[li++]; tcl[ti++] = Convert.ToByte(Math.Round(val * iarr)); }
            }
        }
        static void boxBlurT_4(byte[] scl, byte[] tcl, int w, int h, double r)
        {
            double iarr = 1 / (r + r + 1);
            for (var i = 0; i < w; i++)
            {
                var ti = i;
                var li = ti;
                int ri = Convert.ToInt32(ti + r * w);
                var fv = scl[ti];
                var lv = scl[ti + w * (h - 1)];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += scl[ti + j * w];
                for (var j = 0; j <= r; j++) { val += scl[ri] - fv; tcl[ti] = Convert.ToByte(Math.Round((double)val * iarr)); ri += w; ti += w; }
                for (var j = r + 1; j < h - r; j++) { val += scl[ri] - scl[li]; tcl[ti] = Convert.ToByte(Math.Round((double)val * iarr)); li += w; ri += w; ti += w; }
                for (var j = h - r; j < h; j++) { val += lv - scl[li]; tcl[ti] = Convert.ToByte(Math.Round((double)val * iarr)); li += w; ti += w; }
            }
        }

        static double[] BoxesForGauss(int sigma, int n)  // standard deviation, number of boxes
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);  // Ideal averaging filter width 
            var wl = Math.Floor(wIdeal); if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            var m = Math.Round(mIdeal);
            // var sigmaActual = Math.sqrt( (m*wl*wl + (n-m)*wu*wu - n)/12 );

            var sizes = new double[n];
            for (var i = 0; i < n; i++)
            {
                sizes[i] = (i < m ? wl : wu);
            }
            return sizes;
        }
    }
}
