using SkiaSharp.Views.Windows;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Zebble
{
    public class NativeBoxShadowCanvas : SKXamlCanvas
    {
        BoxShadowCanvas View;

        public NativeBoxShadowCanvas(BoxShadowCanvas canvas)
        {
            View = canvas;
            View.OnDraw.HandleOn(Thread.UI, Invalidate);
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
        FrameworkElement Result;

        public Task<FrameworkElement> Render(Renderer renderer)
        {
            Result = new NativeBoxShadowCanvas((BoxShadowCanvas)renderer.View);
            return Task.FromResult(Result);
        }

        void IDisposable.Dispose() => Result = null;
    }
}
