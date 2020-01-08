using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zebble
{
    internal class GaussianBlur
    {
        readonly int[] Alphas, Reds, Greens, Blues;
        readonly int Width, Height, Radius;
        readonly ParallelOptions Options = new ParallelOptions { MaxDegreeOfParallelism = 16 };

        public GaussianBlur(Color[] colors, int width, int height, int radius)
        {
            Width = width;
            Height = height;
            Radius = radius;

            Alphas = new int[colors.Length];
            Reds = new int[colors.Length];
            Greens = new int[colors.Length];
            Blues = new int[colors.Length];

            Parallel.For(0, colors.Length, Options, i =>
            {
                var color = colors[i];

                Alphas[i] = color.Alpha;
                Reds[i] = color.Red;
                Greens[i] = color.Green;
                Blues[i] = color.Blue;
            });
        }

        public Color[] Blur()
        {
            var newAlpha = new int[Width * Height];
            var newRed = new int[Width * Height];
            var newGreen = new int[Width * Height];
            var newBlue = new int[Width * Height];
            var dest = new Color[Width * Height];

            Parallel.Invoke(
                () => gaussBlur_4(Alphas, newAlpha),
                () => gaussBlur_4(Reds, newRed),
                () => gaussBlur_4(Greens, newGreen),
                () => gaussBlur_4(Blues, newBlue));

            Parallel.For(0, dest.Length, Options, i =>
            {
                if (newAlpha[i] > 255) newAlpha[i] = 255;
                if (newRed[i] > 255) newRed[i] = 255;
                if (newGreen[i] > 255) newGreen[i] = 255;
                if (newBlue[i] > 255) newBlue[i] = 255;

                if (newAlpha[i] < 0) newAlpha[i] = 0;
                if (newRed[i] < 0) newRed[i] = 0;
                if (newGreen[i] < 0) newGreen[i] = 0;
                if (newBlue[i] < 0) newBlue[i] = 0;

                dest[i] = new Color((byte)newRed[i], (byte)newGreen[i], (byte)newBlue[i], (byte)newAlpha[i]);
            });

            return dest;
        }

        void gaussBlur_4(int[] source, int[] dest)
        {
            var bxs = boxesForGauss(Radius, 3);
            boxBlur_4(source, dest, Width, Height, (bxs[0] - 1) / 2);
            boxBlur_4(dest, source, Width, Height, (bxs[1] - 1) / 2);
            boxBlur_4(source, dest, Width, Height, (bxs[2] - 1) / 2);
        }

        int[] boxesForGauss(int sigma, int n)
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
            var wl = (int)Math.Floor(wIdeal);
            if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (double)(12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            var m = Math.Round(mIdeal);

            var sizes = new List<int>();
            for (var i = 0; i < n; i++) sizes.Add(i < m ? wl : wu);
            return sizes.ToArray();
        }

        void boxBlur_4(int[] source, int[] dest, int w, int h, int r)
        {
            for (var i = 0; i < source.Length; i++) dest[i] = source[i];
            boxBlurH_4(dest, source, w, h, r);
            boxBlurT_4(source, dest, w, h, r);
        }

        void boxBlurH_4(int[] source, int[] dest, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, h, Options, i =>
            {
                var ti = i * w;
                var li = ti;
                var ri = ti + r;
                var fv = source[ti];
                var lv = source[ti + w - 1];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j];
                for (var j = 0; j <= r; j++)
                {
                    val += source[ri++] - fv;
                    dest[ti++] = (int)Math.Round(val * iar);
                }
                for (var j = r + 1; j < w - r; j++)
                {
                    val += source[ri++] - dest[li++];
                    dest[ti++] = (int)Math.Round(val * iar);
                }
                for (var j = w - r; j < w; j++)
                {
                    val += lv - source[li++];
                    dest[ti++] = (int)Math.Round(val * iar);
                }
            });
        }

        void boxBlurT_4(int[] source, int[] dest, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, w, Options, i =>
            {
                var ti = i;
                var li = ti;
                var ri = ti + r * w;
                var fv = source[ti];
                var lv = source[ti + w * (h - 1)];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j * w];
                for (var j = 0; j <= r; j++)
                {
                    val += source[ri] - fv;
                    dest[ti] = (int)Math.Round(val * iar);
                    ri += w;
                    ti += w;
                }
                for (var j = r + 1; j < h - r; j++)
                {
                    val += source[ri] - source[li];
                    dest[ti] = (int)Math.Round(val * iar);
                    li += w;
                    ri += w;
                    ti += w;
                }
                for (var j = h - r; j < h; j++)
                {
                    val += lv - source[li];
                    dest[ti] = (int)Math.Round(val * iar);
                    li += w;
                    ti += w;
                }
            });
        }
    }
}