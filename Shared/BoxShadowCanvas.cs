using SkiaSharp;
#if UWP
using SkiaSharp.Views.UWP;
#elif __IOS__
    using SkiaSharp.Views.iOS;
#else
using SkiaSharp.Views.Android;
#endif
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Olive;

namespace Zebble
{
    public class BoxShadowCanvas : View, IRenderedBy<SkiaCanvasRenderer>
    {
        const int TOP_LEFT = 0, TOP_RIGHT = 1, BOTTOM_RIGHT = 2, BOTTOM_LEFT = 3;

        internal Action<SKPaintSurfaceEventArgs> NativeDarwAction;
        internal readonly AsyncEvent OnDraw = new();
        internal readonly AsyncEvent<byte[]> OnExportCompleted = new();
        internal byte[] ImageData;

        TaskCompletionSource<bool> Waiting;
        readonly ConcurrentList<Action<SKPaintSurfaceEventArgs>> DrawActions = new();
        int Radius, Blur;
        float[] ShapeCorners;

        int ShadowMargin => Device.Scale.ToDevice(10);

        public Color Color { get; set; }

        public Task Draw(byte[] imagedata, int blur)
        {
            Blur = Device.Scale.ToDevice(blur);
            ImageData = imagedata;

            NativeDarwAction = e =>
            {
                if (!ImageData.None()) DrawNativeImage(e);
                else
                {
                    foreach (var action in DrawActions) action.Invoke(e);

                    if (BoxShadow.ShouldCache) ExportImage(e);
                    DrawActions.Clear();
                }
            };

            return RaiseOnDraw();
        }

        public Task DrawCircle(int radius)
        {
            Radius = Device.Scale.ToDevice(radius);
            DrawActions.Add(DrawNativeCircle);

            return Task.CompletedTask;
        }

        public Task DrawRect(float[] radius)
        {
            ShapeCorners = radius;
            DrawActions.Add(DrawNativeRect);

            return Task.CompletedTask;
        }

        Task RaiseOnDraw()
        {
            if (!IsRendered())
            {
                Waiting = new TaskCompletionSource<bool>();

                Waiting.Task.ContinueWith(t =>
                {
                    if (t.IsCompleted)
                        OnDraw.Raise();
                });

                return Waiting.Task;
            }
            else return OnDraw.Raise();
        }

        void DrawNativeImage(SKPaintSurfaceEventArgs e)
        {
            e.Surface.Canvas.Clear();

            var bitmap = SKBitmap.Decode(ImageData);
            e.Surface.Canvas.DrawBitmap(bitmap, e.Info.Rect);
        }

        void DrawNativeCircle(SKPaintSurfaceEventArgs e)
        {
            e.Surface.Canvas.Clear();
            e.Surface.Canvas.DrawCircle(e.Info.Width / 2, e.Info.Height / 2, Radius, new SKPaint
            {
                Style = SKPaintStyle.Fill,
                StrokeWidth = 1,
                Color = Color.Render().ToSKColor(),
                ImageFilter = SKImageFilter.CreateBlur(Blur, Blur)
            });
        }

        void DrawNativeRect(SKPaintSurfaceEventArgs e)
        {
            e.Surface.Canvas.Clear();
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                StrokeWidth = 1,
                Color = Color.Render().ToSKColor(),
                ImageFilter = SKImageFilter.CreateBlur(Blur, Blur)
            };

            var rect = new SKRect(ShadowMargin, ShadowMargin, e.Info.Width - ShadowMargin, e.Info.Height - ShadowMargin);

            if (ShapeCorners.Distinct().IsSingle())
            {
                var singleRadius = Device.Scale.ToDevice(ShapeCorners.First());
                e.Surface.Canvas.DrawRoundRect(rect, singleRadius, singleRadius, paint);
            }
            else
            {
                var topLeft = Device.Scale.ToDevice(ShapeCorners[TOP_LEFT]);
                var topRight = Device.Scale.ToDevice(ShapeCorners[TOP_RIGHT]);
                var bottomLeft = Device.Scale.ToDevice(ShapeCorners[BOTTOM_LEFT]);
                var bottomRight = Device.Scale.ToDevice(ShapeCorners[BOTTOM_RIGHT]);

                var path = RoundedRectPath(rect, topLeft, topRight, bottomRight, bottomLeft);
                e.Surface.Canvas.DrawPath(path, paint);
            }
        }

        void ExportImage(SKPaintSurfaceEventArgs e)
        {
            using (var image = e.Surface.Snapshot())
            {
                ImageData = image.Encode(SKEncodedImageFormat.Png, 80).ToArray();
                OnExportCompleted.Raise(ImageData);
            }
        }

        SKPath RoundedRectPath(SKRect rect, float topLeftRadius, float topRightRadius, float bottomRightRadius, float bottomLeftRadius)
        {
            var path = new SKPath();
            topLeftRadius = topLeftRadius < 0 ? 0 : topLeftRadius;
            topRightRadius = topRightRadius < 0 ? 0 : topRightRadius;
            bottomLeftRadius = bottomLeftRadius < 0 ? 0 : bottomLeftRadius;
            bottomRightRadius = bottomRightRadius < 0 ? 0 : bottomRightRadius;

            path.MoveTo(rect.Left + topLeftRadius, rect.Top);
            path.LineTo(rect.Right - topRightRadius, rect.Top);
            path.QuadTo(rect.Right, rect.Top, rect.Right, rect.Top + topRightRadius);
            path.LineTo(rect.Right, rect.Bottom - bottomRightRadius);
            path.QuadTo(rect.Right, rect.Bottom, rect.Right - bottomRightRadius, rect.Bottom);
            path.LineTo(rect.Left + bottomLeftRadius, rect.Bottom);
            path.QuadTo(rect.Left, rect.Bottom, rect.Left, rect.Bottom - bottomLeftRadius);
            path.LineTo(rect.Left, rect.Top + topLeftRadius);
            path.QuadTo(rect.Left, rect.Top, rect.Left + topLeftRadius, rect.Top);
            path.Close();

            return path;
        }

        public override async Task OnRendered()
        {
            await base.OnRendered();

            Waiting?.TrySetResult(true);
        }

        public override void Dispose()
        {
            DrawActions.Clear();
            NativeDarwAction = null;
            Waiting = null;
            ImageData = null;

            base.Dispose();
        }
    }
}
