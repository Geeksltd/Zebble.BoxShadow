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

            BackgroundColor = UIColor.Clear;
            Opaque = false;
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            View.NativeDarwAction?.Invoke(e);
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