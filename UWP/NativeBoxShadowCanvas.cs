using SkiaSharp.Views.UWP;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Zebble
{
    public class NativeBoxShadowCanvas : SKXamlCanvas
    {
        BoxShadowCanvas View;

        public NativeBoxShadowCanvas(BoxShadowCanvas canvas)
        {
            View = canvas;
            View.OnDraw.Handle(() => Invalidate());
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
        FrameworkElement Result;

        public Task<FrameworkElement> Render(Renderer renderer)
        {
            Result = new NativeBoxShadowCanvas((BoxShadowCanvas)renderer.View);
            return Task.FromResult(Result);
        }

        void IDisposable.Dispose()
        {
            Result = null;
        }
    }
}
