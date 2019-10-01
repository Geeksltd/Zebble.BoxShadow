namespace Zebble
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class BoxShadow : ImageView
    {
        const string AlgorithmVersion = "v4";

        const int SHADOW_MARGIN = 10, TOP_LEFT = 0, TOP_RIGHT = 1, BOTTOM_RIGHT = 2, BOTTOM_LEFT = 3;
        const double FULL_CIRCLE_DEGREE = 360.0, HALF_CIRCLE_DEGREE = 180.0;
        static ConcurrentDictionary<string, AsyncLock> CreationLocks = new ConcurrentDictionary<string, AsyncLock>();
        static List<KeyValuePair<string, byte[]>> RenderedShadows = new List<KeyValuePair<string, byte[]>>();
        View Owner;
        readonly List<CornerPosition> DrawnCorners = new List<CornerPosition> { CornerPosition.None };
        AsyncLock RenderSyncLock = new AsyncLock();

        public BoxShadow()
        {
            Absolute = true;
            Color = Colors.Gray;
            BlurRadius = 3;
        }

        string CurrentFileName
        {
            get
            {
                var size = $"{Owner.ActualWidth}x{Owner.ActualHeight}";
                var shadow = $"s{XOffset},{YOffset},{BlurRadius},{Expand}";
                var border = Owner.Border.Get(b => $"b{b.RadiusTopLeft},{b.RadiusTopRight},{b.RadiusBottomRight},{b.RadiusBottomLeft}");

                var margin = Owner.Margin;
                var padding = Owner.Parent.Padding;

                var position = $"p{margin.Top.CurrentValue},{margin.Left.CurrentValue},{padding.Top.CurrentValue},{padding.Left.CurrentValue}";

                var color = Color.ToString().TrimStart("#");

                return $"{size} {shadow} {border} {position} {color}";
            }
        }

        DirectoryInfo Folder => Device.IO.Directory($"zebble-box-shadow\\{AlgorithmVersion}-{Id}");

        FileInfo CurrentFile => Folder.EnsureExists().GetFile(CurrentFileName + ".png");

        public View For
        {
            get => Owner;
            set
            {
                if (Owner == value) return;
                if (Owner != null)
                    Device.Log.Error("Shadow.For cannot be changed once it's set.");
                else
                    Owner = value;
            }
        }

        public Color Color { get; set; }

        public int XOffset { get; set; }

        public int YOffset { get; set; }

        public int Expand { get; set; }

        public int BlurRadius { get; set; }

        public override async Task OnPreRender()
        {
            await base.OnPreRender();

            if (Owner == null)
            {
                Device.Log.Error("'For' should be specified for a Shadow.");
                return;
            }

            Height.BindTo(Owner.Height, h => h + (BlurRadius + SHADOW_MARGIN + Expand.LimitMin(0)) * 2);
            Width.BindTo(Owner.Width, w => w + (BlurRadius + SHADOW_MARGIN + Expand.LimitMin(0)) * 2);

            await SyncWithOwner();

            Owner.Height.Changed.Handle(SyncWithOwner);
            Owner.Width.Changed.Handle(SyncWithOwner);
            Owner.VisibilityChanged.Handle(SyncWithOwner);
        }

        public async override Task OnRendered()
        {
            X.BindTo(Owner.X, Owner.Margin.Left, Owner.Parent.Padding.Left, (x, a, b) =>
                Math.Max(x, a) + XOffset + b - (SHADOW_MARGIN + BlurRadius + Owner.Border.Left)
            );

            Y.BindTo(Owner.Y, y => y + YOffset - (SHADOW_MARGIN + BlurRadius + Owner.Border.Top));

            await base.OnRendered();
        }

        async Task SyncWithOwner()
        {
            Visible = Owner.Visible;
            try
            {
                using (await RenderSyncLock.LockAsync())
                {
                    if (RenderedShadows.None(x => x.Key == CurrentFileName))
                    {
                        var target = await CreateImageFile();
                        ImageData = target.ReadAllBytes();
                        RenderedShadows.Add(new KeyValuePair<string, byte[]>(CurrentFileName, ImageData));
                    }
                    else ImageData = RenderedShadows.FirstOrDefault(x => x.Key == CurrentFileName).Value;
                }
            }
            catch (Exception ex) { Device.Log.Error(ex.Message); }
        }

        async Task<FileInfo> CreateImageFile()
        {
            var file = CurrentFile;

            if (!file.Exists())
            {
                var creationLock = CreationLocks.GetOrAdd(file.FullName, x => new AsyncLock());

                using (await creationLock.LockAsync())
                    if (!file.Exists()) await DoCreateImageFile();
            }

            return file;
        }

        async Task DoCreateImageFile()
        {
            var width = (int)Width.CurrentValue;
            var height = (int)Height.CurrentValue;

            var colors = await CreateMatrixColours(width, height);

            var imageData = await SaveAsPng(width, height, colors).DropContext();
            await CurrentFile.WriteAllBytesAsync(imageData);
        }

        async Task<Color[]> CreateMatrixColours(int width, int height)
        {
            var colors = Enumerable.Repeat(Colors.Transparent, height * width).ToArray();

            var borderRadius = new float[] {
                Owner.Border.RadiusTopLeft,
                Owner.Border.RadiusTopRight,
                Owner.Border.RadiusBottomRight,
                Owner.Border.RadiusBottomLeft
            };

            if (borderRadius.Sum() != 0)
            {
                var ownerWidth = Owner.Width.CurrentValue;
                var ownerHeight = Owner.Height.CurrentValue;

                if (borderRadius.Distinct().IsSingle() && ownerWidth.AlmostEquals(ownerHeight) && (int)borderRadius[0] == ownerWidth / 2)
                {
                    var stroke = GetStrokeByPosition(CornerPosition.TopLeft);
                    await DrawCircle(colors, stroke, width, height / 2);
                }
                else
                {
                    await DrawRectangle(colors, width, height, borderRadius);

                    if (ownerHeight > ownerWidth || ownerWidth > ownerHeight)
                        await DrawCylinder(colors, width, height, borderRadius);
                    else
                        await DrawCornerCirlcles(colors, width, height);
                }
            }
            else await DrawRectangle(colors, width, height, borderRadius);

            return GaussianBlur.Blur(colors, width, height, BlurRadius);
        }

        Rec GetCorner(int width, int height, CornerPosition corner)
        {
            Rec resutl = null;
            switch (corner)
            {
                case CornerPosition.TopLeft:
                    resutl = new Rec
                    {
                        StartX = SHADOW_MARGIN - Expand,
                        StartY = SHADOW_MARGIN - Expand,
                        EndX = SHADOW_MARGIN - Expand + (int)Owner.Border.RadiusTopLeft / 2,
                        EndY = SHADOW_MARGIN - Expand + (int)Owner.Border.RadiusTopLeft / 2
                    };
                    break;
                case CornerPosition.TopRight:
                    resutl = new Rec
                    {
                        StartX = width - SHADOW_MARGIN + Expand - (int)Owner.Border.RadiusTopRight / 2,
                        StartY = SHADOW_MARGIN - Expand,
                        EndX = width + Expand,
                        EndY = SHADOW_MARGIN - Expand + (int)Owner.Border.RadiusTopRight / 2
                    };
                    break;
                case CornerPosition.BottomLeft:
                    resutl = new Rec
                    {
                        StartX = SHADOW_MARGIN - Expand,
                        StartY = height - SHADOW_MARGIN + Expand - (int)Owner.Border.RadiusBottomLeft / 2,
                        EndX = SHADOW_MARGIN - Expand + (int)Owner.Border.RadiusBottomLeft / 2,
                        EndY = height - SHADOW_MARGIN + Expand
                    };
                    break;
                case CornerPosition.BottomRight:
                    resutl = new Rec
                    {
                        StartX = width - SHADOW_MARGIN + Expand - (int)Owner.Border.RadiusBottomRight / 2,
                        StartY = height - SHADOW_MARGIN + Expand - (int)Owner.Border.RadiusBottomRight / 2,
                        EndX = width - SHADOW_MARGIN + Expand,
                        EndY = height - SHADOW_MARGIN + Expand
                    };
                    break;
                default: break;
            }

            return resutl;
        }

        async Task DrawRectangle(Color[] colors, int width, int height, float[] borderRadius)
        {
            var topLeft = GetCorner(width, height, CornerPosition.TopLeft);
            var topRight = GetCorner(width, height, CornerPosition.TopRight);
            var bottomLeft = GetCorner(width, height, CornerPosition.BottomLeft);
            var bottomRight = GetCorner(width, height, CornerPosition.BottomRight);

            var minY = SHADOW_MARGIN;
            var maxY = height - SHADOW_MARGIN;
            if (Expand < 0) { minY -= Expand; maxY += Expand; }

            var minX = SHADOW_MARGIN;
            var maxX = width - SHADOW_MARGIN;
            if (Expand < 0) { minX -= Expand; maxX += Expand; }

            var hasBorder = borderRadius.Sum() != 0;

            for (var y = minY; y < maxY; y++)
                for (var x = minX; x < maxX; x++)
                {
                    var index = Math.Abs(y * width + x);
                    if (hasBorder)
                    {
                        if ((x >= topLeft.StartX && x <= SHADOW_MARGIN + borderRadius[TOP_LEFT] - 1) &&
                            (y >= topLeft.StartY && y <= SHADOW_MARGIN + borderRadius[TOP_LEFT]))
                        {
                            colors[index] = Colors.Transparent;
                        }
                        else if ((x >= width - (SHADOW_MARGIN + borderRadius[TOP_RIGHT]) && x <= topRight.EndX) &&
                            (y >= topRight.StartY && y <= SHADOW_MARGIN + borderRadius[TOP_RIGHT] - 1))
                        {
                            colors[index] = Colors.Transparent;
                        }
                        else if ((x >= bottomLeft.StartX && x <= SHADOW_MARGIN + borderRadius[BOTTOM_LEFT]) &&
                            (y >= height - (SHADOW_MARGIN + borderRadius[BOTTOM_LEFT]) && y <= bottomLeft.EndY))
                        {
                            colors[index] = Colors.Transparent;
                        }
                        else if ((x >= width - (SHADOW_MARGIN + borderRadius[BOTTOM_RIGHT]) && x <= bottomRight.EndX) &&
                            (y >= height - (SHADOW_MARGIN + borderRadius[BOTTOM_RIGHT]) && y <= bottomRight.EndY))
                        {
                            colors[index] = Colors.Transparent;
                        }
                        else
                            colors[index] = Color;
                    }
                    else
                        colors[index] = Color;
                }

            await Task.CompletedTask;
        }

        async Task DrawCustomArc(Color[] source, int width, int height, CornerPosition cornerPosition)
        {
            var radius = (int)GetRadiusByPosition(cornerPosition);
            var stroke = GetStrokeByPosition(cornerPosition) - BlurRadius;

            switch (cornerPosition)
            {
                case CornerPosition.TopLeft:
                    await DrawCircle(source, radius, width, SHADOW_MARGIN - Expand + radius, -1, 180.0, 270.0);
                    break;
                case CornerPosition.TopRight:
                    await DrawCircle(source, radius, width, width - SHADOW_MARGIN - radius + Expand, SHADOW_MARGIN + radius - Expand, 270.0, 360.0);
                    break;
                case CornerPosition.BottomRight:
                    await DrawCircle(source, radius, width, width - SHADOW_MARGIN - radius + Expand, height - SHADOW_MARGIN - radius + Expand, 0.0, 90.0);
                    break;
                case CornerPosition.BottomLeft:
                    await DrawCircle(source, radius, width, SHADOW_MARGIN + radius - Expand, height - SHADOW_MARGIN - radius + Expand, 90.0, 180.0);
                    break;
                default: break;
            }
        }

        async Task DrawCornerCirlcles(Color[] source, int width, int height)
        {
            if (GetRadiusByPosition(CornerPosition.TopLeft) != 0 && !DrawnCorners.Contains(CornerPosition.TopLeft))
                await DrawCustomArc(source, width, 0, CornerPosition.TopLeft);
            if (GetRadiusByPosition(CornerPosition.TopRight) != 0 && !DrawnCorners.Contains(CornerPosition.TopRight))
                await DrawCustomArc(source, width, 0, CornerPosition.TopRight);
            if (GetRadiusByPosition(CornerPosition.BottomRight) != 0 && !DrawnCorners.Contains(CornerPosition.BottomRight))
                await DrawCustomArc(source, width, height, CornerPosition.BottomRight);
            if (GetRadiusByPosition(CornerPosition.BottomLeft) != 0 && !DrawnCorners.Contains(CornerPosition.BottomLeft))
                await DrawCustomArc(source, width, height, CornerPosition.BottomLeft);
        }

        Task DrawCircle(Color[] source, int stroke, int width, int centerX, int centerY = -1, double startDegree = 0.0, double endDegree = FULL_CIRCLE_DEGREE)
        {
            int xPos = 0, yPos = 0;
            double circles = 1;

            if (centerY == -1) centerY = centerX;

            for (var j = 1; j < stroke; j++)
            {
                circles += 1;
                for (var i = startDegree; i < endDegree; i += 0.1)
                {
                    var angle = i * Math.PI / HALF_CIRCLE_DEGREE;
                    xPos = (int)(centerX + circles * Math.Cos(angle));
                    yPos = (int)(centerY + circles * Math.Sin(angle));

                    var index = Math.Abs(yPos * width + xPos);
                    if (index >= source.Length) continue;
                    source[index] = Color;
                }
            }

            return Task.CompletedTask;
        }

        Task DrawSemicircular(Color[] source, int width, int height, CornerPosition corner, bool isStand)
        {
            int centerX, centerY, stroke;
            double startDegree, endDegree;

            Func<Task> drawCorner = () => DrawCornerCirlcles(source, width, height);

            if (isStand)
            {
                if (corner == CornerPosition.TopLeft)
                {
                    if (!Owner.Width.CurrentValue.AlmostEquals(Owner.Border.RadiusTopLeft * 2)) return drawCorner.Invoke();

                    startDegree = HALF_CIRCLE_DEGREE;
                    endDegree = FULL_CIRCLE_DEGREE;
                    stroke = GetStrokeByPosition(CornerPosition.TopLeft) - BlurRadius;
                    centerX = centerY = width / 2;

                    DrawnCorners.Add(CornerPosition.TopLeft);
                    DrawnCorners.Add(CornerPosition.TopRight);
                }
                else
                {
                    if (!Owner.Width.CurrentValue.AlmostEquals(Owner.Border.RadiusBottomLeft * 2)) return drawCorner.Invoke();

                    startDegree = 0.0;
                    endDegree = HALF_CIRCLE_DEGREE;
                    stroke = GetStrokeByPosition(CornerPosition.BottomLeft) - BlurRadius;
                    centerX = width / 2;
                    centerY = height - (SHADOW_MARGIN + stroke);

                    DrawnCorners.Add(CornerPosition.BottomLeft);
                    DrawnCorners.Add(CornerPosition.BottomRight);
                }
            }
            else
            {
                if (corner == CornerPosition.TopLeft)
                {
                    if (!Owner.Height.CurrentValue.AlmostEquals(Owner.Border.RadiusTopLeft * 2)) return drawCorner.Invoke();

                    startDegree = 90.0;
                    endDegree = 270.0;
                    stroke = GetStrokeByPosition(CornerPosition.TopLeft) - BlurRadius;
                    centerX = centerY = SHADOW_MARGIN + stroke;

                    DrawnCorners.Add(CornerPosition.TopLeft);
                    DrawnCorners.Add(CornerPosition.BottomLeft);
                }
                else
                {
                    if (!Owner.Height.CurrentValue.AlmostEquals(Owner.Border.RadiusTopRight * 2)) return drawCorner.Invoke();

                    startDegree = 270.0;
                    endDegree = 450.0;
                    stroke = GetStrokeByPosition(CornerPosition.BottomLeft) - BlurRadius;
                    centerX = width - (SHADOW_MARGIN + stroke);
                    centerY = SHADOW_MARGIN + stroke;

                    DrawnCorners.Add(CornerPosition.TopRight);
                    DrawnCorners.Add(CornerPosition.BottomRight);
                }
            }

            return DrawCircle(source, stroke, width, centerX, centerY, startDegree, endDegree);
        }

        Task DrawCylinder(Color[] source, int width, int height, float[] radius)
        {
            if (height > width)
            {
                if (radius[TOP_LEFT] != 0 && radius[TOP_LEFT].AlmostEquals(radius[TOP_RIGHT]))
                {
                    DrawSemicircular(source, width, height, CornerPosition.TopLeft, isStand: true);
                }

                if (radius[BOTTOM_LEFT] != 0 && radius[BOTTOM_LEFT].AlmostEquals(radius[BOTTOM_RIGHT]))
                {
                    DrawSemicircular(source, width, height, CornerPosition.BottomLeft, isStand: true);
                }
            }
            else
            {
                if (radius[TOP_LEFT] != 0 && radius[TOP_LEFT].AlmostEquals(radius[BOTTOM_LEFT]))
                {
                    DrawSemicircular(source, width, height, CornerPosition.TopLeft, isStand: false);
                }

                if (radius[TOP_RIGHT] != 0 && radius[TOP_RIGHT].AlmostEquals(radius[BOTTOM_RIGHT]))
                {
                    DrawSemicircular(source, width, height, CornerPosition.TopRight, isStand: false);
                }
            }

            return Task.CompletedTask;
        }

        int GetStrokeByPosition(CornerPosition position)
        {
            switch (position)
            {
                case CornerPosition.TopLeft: return (int)Owner.Border.RadiusTopLeft;
                case CornerPosition.TopRight: return (int)Owner.Border.RadiusTopRight;
                case CornerPosition.BottomRight: return (int)Owner.Border.RadiusBottomRight - 1;
                default: return (int)Owner.Border.RadiusBottomLeft;
            }
        }

        float GetRadiusByPosition(CornerPosition position)
        {
            switch (position)
            {
                case CornerPosition.TopLeft: return Owner.Border.RadiusTopLeft;
                case CornerPosition.TopRight: return Owner.Border.RadiusTopRight;
                case CornerPosition.BottomRight: return Owner.Border.RadiusBottomRight;
                default: return Owner.Border.RadiusBottomLeft;
            }
        }
    }

    internal class Rec
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
    }

    internal enum CornerPosition
    {
        None = -1,
        TopLeft = 0,
        TopRight = 1,
        BottomRight = 2,
        BottomLeft = 3
    }
}