using SkiaSharp.Views.iOS;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UIKit;

namespace Zebble
{
    public class NativeBoxShadowCanvas : SKCanvasView
    {
        BoxShadowCanvas View;

        public NativeBoxShadowCanvas(BoxShadowCanvas canvas)
        {
            View = canvas;
            View.OnDraw.Handle(() => SetNeedsDisplay());
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            View.DrawNativeImage(e);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SkiaCanvasRenderer : INativeRenderer
    {
        UIView Result;

        public Task<UIView> Render(Renderer renderer)
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