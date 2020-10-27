using SkiaSharp;
#if UWP
using SkiaSharp.Views.UWP;
#elif __IOS__
    using SkiaSharp.Views.iOS;
#else
    using SkiaSharp.Views.Android;
#endif
using System;
using System.Threading.Tasks;

namespace Zebble
{
    public class BoxShadowCanvas : View, IRenderedBy<SkiaCanvasRenderer>
    {
        TaskCompletionSource<bool> Waiting;

        internal readonly AsyncEvent OnDraw = new AsyncEvent();
        internal byte[] ImageData, OutputImageData;
        internal int Blur;

        public Task DrawImage(byte[] imagedata, int blur)
        {
            Waiting = new TaskCompletionSource<bool>();

            Blur = blur;
            ImageData = imagedata;

            Waiting.Task.ContinueWith(t =>
            {
                if (t.IsCompleted)
                    OnDraw.Raise();
            });

            return Waiting.Task;
        }

        public void DrawNativeImage(SKPaintSurfaceEventArgs e)
        {
            e.Surface.Canvas.Clear();

            var bitmap = SKBitmap.Decode(ImageData);
            e.Surface.Canvas.DrawBitmap(bitmap, e.Info.Rect , new SKPaint { ImageFilter = SKImageFilter.CreateBlur(Blur, Blur) });
        }

        public override async Task OnRendered()
        {
            await base.OnRendered();

            Waiting?.TrySetResult(true);
        }

        public override void Dispose()
        {
            base.Dispose();

            Waiting = null;
            ImageData = null;
            OutputImageData = null;
        }
    }
}
