namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class BoxShadow : ImageView
    {
        const int SHADOW_MARGIN = 10;
        const int TOP_LEFT = 0, TOP_RIGHT = 1, BOTTOM_RIGHT = 2, BOTTOM_LEFT = 3;
        const double FULL_CIRCLE_DEGREE = 360.0, HALF_CIRCLE_DEGREE = 180.0;

        View Owner;
        readonly List<CornerPosition> DrawnCorners = new List<CornerPosition> { CornerPosition.None };
        bool IsRunning = false;
        int IncreaseValue;

        FileInfo CurrentFile
        {
            get
            {
                var name = new object[] { Owner.ActualX, Owner.ActualY, Owner.Border.RadiusBottomLeft, Owner.Border.RadiusBottomRight,
                    Owner.Border.RadiusTopLeft, Owner.Border.RadiusTopRight, Owner.Width, Owner.Height,BlurRadius,XOffset,YOffset }.ToString("|").ToIOSafeHash();
                return Device.IO.GetTempRoot().GetFile($"{name}.png");
            }
        }

        public BoxShadow() => Absolute = true;
        public View For
        {
            get => Owner;
            set
            {
                if (Owner == value) return;
                if (Owner != null)
                {
                    Device.Log.Error("Shadow.For cannot be changed once it's set.");
                    return;
                }

                Owner = value;
            }
        }
        public Color Color { get; set; } = Colors.Gray;
        public int XOffset { get; set; } = 0;
        public int YOffset { get; set; } = 0;
        public int BlurRadius { get; set; } = 3;

        public override async Task OnPreRender()
        {
            await base.OnPreRender();

            if (Owner == null)
            {
                Device.Log.Error("'For' should be specified for a Shadow.");
                return;
            }

            // Attach to the owner:
            await SyncWithOwner();
            Owner.X.Changed.Handle(SyncWithOwner);
            Owner.Y.Changed.Handle(SyncWithOwner);
            Owner.Height.Changed.Handle(SyncWithOwner);
            Owner.Width.Changed.Handle(SyncWithOwner);
            Owner.VisibilityChanged.Handle(SyncWithOwner);
        }

        public override async Task OnInitialized()
        {
            await WhenShown(() =>
            {
                Device.UIThread.Run(() =>
                {
#if UWP
                    var native = this.Native();
                    native.IsHitTestVisible = false;
#endif
                });
            });
        }

        public async override Task OnRendered()
        {
            X.BindTo(Owner.X, x => (x - (SHADOW_MARGIN + BlurRadius)) + Owner.Border.Left + XOffset);
            Y.BindTo(Owner.Y, y => (y - (SHADOW_MARGIN + BlurRadius)) + Owner.Border.Top + YOffset);

            await base.OnRendered();
        }

        async Task SyncWithOwner()
        {
            IncreaseValue = BlurRadius * 2;
            var height = Height.CurrentValue;
            var width = Width.CurrentValue;

            Height.Set(Owner.Height.CurrentValue + IncreaseValue + SHADOW_MARGIN * 2);
            Width.Set(Owner.Width.CurrentValue + IncreaseValue + SHADOW_MARGIN * 2);

            if (IsRunning && (Math.Abs(height - Height.CurrentValue) > 2 || Math.Abs(width - Width.CurrentValue) > 2))
                await IsEnd();
            else if (IsRunning)
                return;

            IsRunning = true;

            Visible = Owner.Visible;
            Opacity = Owner.Opacity;

            try
            {
                if (Visible)
                {
                    var target = await CreateImageFile();
                    BackgroundImagePath = target.FullName;
                }
            }
            catch (Exception ex) { Device.Log.Error(ex.Message); }
            IsRunning = false;
        }

        async Task IsEnd()
        {
            while (IsRunning)
                await Task.Delay(25);
        }

        async Task<FileInfo> CreateImageFile()
        {
            if (CurrentFile.Exists) return CurrentFile;

            var height = (int)Height.CurrentValue;
            var width = (int)Width.CurrentValue;

            var length = height * width;
            var colors = Enumerable.Repeat(Colors.Transparent, length).ToArray();

            var borderRadius = new float[] { Owner.Border.RadiusTopLeft, Owner.Border.RadiusTopRight, Owner.Border.RadiusBottomRight, Owner.Border.RadiusBottomLeft };
            if (borderRadius.Sum() != 0)
            {
                var ownerWidth = Owner.Width.CurrentValue;
                var ownerHeight = Owner.Height.CurrentValue;

                if (borderRadius.Distinct().IsSingle() && ownerWidth.AlmostEquals(ownerHeight) && (int)borderRadius.First() == ownerWidth / 2)
                {
                    var stroke = GetStrokeByPosition(CornerPosition.TopLeft);
                    await DrawCircle(colors, stroke, width, height / 2);
                }
                else
                {
                    await DrawRectangle(colors, width, height, borderRadius);

                    if (ownerHeight > ownerWidth || ownerWidth > ownerHeight)
                    {
                        await DrawCylinder(colors, width, height, borderRadius);
                    }
                    else
                    {
                        await DrawCornerCirlcles(colors, width, height);
                    }
                }
            }
            else await DrawRectangle(colors, width, height, borderRadius);

            colors = GaussianBlur.Blur(colors, width, height, BlurRadius);

            return await SaveAsPng(width, height, colors);
        }

        Task<Rec> GetCorner(int width, int height, CornerPosition corner)
        {
            Rec resutl = null;
            switch (corner)
            {
                case CornerPosition.TopLeft:
                    resutl = new Rec
                    {
                        StartX = SHADOW_MARGIN,
                        EndX = IncreaseValue + SHADOW_MARGIN + (int)Owner.Border.RadiusTopLeft / 2,
                        StartY = SHADOW_MARGIN,
                        EndY = IncreaseValue + SHADOW_MARGIN + (int)Owner.Border.RadiusTopLeft / 2
                    };
                    break;
                case CornerPosition.TopRight:
                    resutl = new Rec
                    {
                        StartX = (width - SHADOW_MARGIN) - IncreaseValue - (int)Owner.Border.RadiusTopRight / 2,
                        EndX = width,
                        StartY = SHADOW_MARGIN,
                        EndY = IncreaseValue + SHADOW_MARGIN + (int)Owner.Border.RadiusTopRight / 2
                    };
                    break;
                case CornerPosition.BottomLeft:
                    resutl = new Rec
                    {
                        StartX = SHADOW_MARGIN,
                        EndX = IncreaseValue + SHADOW_MARGIN + (int)Owner.Border.RadiusBottomLeft / 2,
                        StartY = (height - SHADOW_MARGIN) - IncreaseValue - (int)Owner.Border.RadiusBottomLeft / 2,
                        EndY = height - SHADOW_MARGIN
                    };
                    break;
                case CornerPosition.BottomRight:
                    resutl = new Rec
                    {
                        StartX = (width - SHADOW_MARGIN) - IncreaseValue - (int)Owner.Border.RadiusBottomRight / 2,
                        EndX = width - SHADOW_MARGIN,
                        StartY = (height - SHADOW_MARGIN) - IncreaseValue - (int)Owner.Border.RadiusBottomRight / 2,
                        EndY = height - SHADOW_MARGIN
                    };
                    break;
                default: break;
            }

            return Task.FromResult(resutl);
        }

        async Task DrawRectangle(Color[] colors, int width, int height, float[] borderRadius)
        {
            var topLeft = await GetCorner(width, height, CornerPosition.TopLeft);
            var topRight = await GetCorner(width, height, CornerPosition.TopRight);
            var bottomLeft = await GetCorner(width, height, CornerPosition.BottomLeft);
            var bottomRight = await GetCorner(width, height, CornerPosition.BottomRight);

            for (var y = SHADOW_MARGIN; y < height - SHADOW_MARGIN; y++)
                for (var x = SHADOW_MARGIN; x < width - SHADOW_MARGIN; x++)
                {
                    int index = Math.Abs(y * width + x);
                    if (borderRadius.Sum() != 0)
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
                    await DrawCircle(source, radius, width, SHADOW_MARGIN + radius, -1, 180.0, 270.0);
                    break;
                case CornerPosition.TopRight:
                    await DrawCircle(source, radius, width, width - (SHADOW_MARGIN + radius), SHADOW_MARGIN + radius, 270.0, 360.0);
                    break;
                case CornerPosition.BottomRight:
                    await DrawCircle(source, radius, width, width - (SHADOW_MARGIN + radius), height - (SHADOW_MARGIN + radius), 0.0, 90.0);
                    break;
                case CornerPosition.BottomLeft:
                    await DrawCircle(source, radius, width, SHADOW_MARGIN + radius, height - (SHADOW_MARGIN + radius), 90.0, 180.0);
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

            for (int j = 1; j < stroke; j++)
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
            int result;
            switch (position)
            {
                case CornerPosition.TopLeft:
                    result = IncreaseValue + (int)Owner.Border.RadiusTopLeft;
                    break;
                case CornerPosition.TopRight:
                    result = IncreaseValue + (int)Owner.Border.RadiusTopRight;
                    break;
                case CornerPosition.BottomRight:
                    result = IncreaseValue + (int)Owner.Border.RadiusBottomRight - 1;
                    break;
                default:
                    result = IncreaseValue + (int)Owner.Border.RadiusBottomLeft;
                    break;
            }

            return result;
        }

        float GetRadiusByPosition(CornerPosition position)
        {
            float result;
            switch (position)
            {
                case CornerPosition.TopLeft:
                    result = Owner.Border.RadiusTopLeft;
                    break;
                case CornerPosition.TopRight:
                    result = Owner.Border.RadiusTopRight;
                    break;
                case CornerPosition.BottomRight:
                    result = Owner.Border.RadiusBottomRight;
                    break;
                default:
                    result = Owner.Border.RadiusBottomLeft;
                    break;
            }

            return result;
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