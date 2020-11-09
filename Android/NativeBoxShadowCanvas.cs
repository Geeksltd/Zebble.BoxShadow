using SkiaSharp.Views.Android;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Zebble
{
    internal class NativeBoxShadowCanvas : SKCanvasView
    {
        BoxShadowCanvas View;

        public NativeBoxShadowCanvas(BoxShadowCanvas canvas) : base(UIRuntime.CurrentActivity)
        {
            View = canvas;
            View.OnDraw.HandleOn(Thread.UI, () => Invalidate());
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            View.NativeDarwAction?.Invoke(e);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class SkiaCanvasRenderer : INativeRenderer
    {
        Android.Views.View Result;

        public Task<Android.Views.View> Render(Renderer renderer)
        {
            Result = new NativeBoxShadowCanvas((BoxShadowCanvas)renderer.View);
            return Task.FromResult(Result);
        }

        void IDisposable.Dispose()
        {
            Result?.Dispose();
            Result = null;
        }
    }
}
