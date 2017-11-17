namespace Zebble
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class BoxShadow : ImageView
    {
        const int SHADOW_MARGIN = 10;
        const int TOP_LEFT = 0, TOP_RIGHT = 1, BOTTOM_RIGHT = 2, BOTTOM_LEFT = 3;
        const double FULL_CIRCLE_DEGREE = 360.0, HALF_CIRCLE_DEGREE = 180.0;

        View Owner;
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
            if (Owner.BackgroundColor == Colors.Transparent || Owner.BackgroundImagePath != null || Owner.BackgroundImageData != null)
                Owner.Background(color: Colors.White);

            X.Set((Owner.ActualX - SHADOW_MARGIN) + (XOffset - Owner.Border.TotalHorizontal));
            Y.Set((Owner.ActualY - SHADOW_MARGIN) + (YOffset - Owner.Border.TotalVertical));

            await Owner.BringToFront();

            await base.OnRendered();
        }

        async Task SyncWithOwner()
        {
            var increaseValue = BlurRadius * 2;
            var height = Height.CurrentValue;
            var width = Width.CurrentValue;

            Height.Set(Owner.Height.CurrentValue + increaseValue + SHADOW_MARGIN * 2);
            Width.Set(Owner.Width.CurrentValue + increaseValue + SHADOW_MARGIN * 2);

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

            IncreaseValue = BlurRadius * 2;

            var height = (int)Height.CurrentValue;
            var width = (int)Width.CurrentValue;

            var length = height * width;
            var colors = Enumerable.Repeat(Colors.Transparent, length).ToArray();

            var topLeft = await GetCorner(IncreaseValue, width, height, TOP_LEFT);
            var topRight = await GetCorner(IncreaseValue, width, height, TOP_RIGHT);
            var bottomLeft = await GetCorner(IncreaseValue, width, height, BOTTOM_RIGHT);
            var bottomRight = await GetCorner(IncreaseValue, width, height, BOTTOM_LEFT);

            for (var y = SHADOW_MARGIN; y < height - SHADOW_MARGIN; y++)
                for (var x = SHADOW_MARGIN; x < width - SHADOW_MARGIN; x++)
                {
                    int index = Math.Abs(y * width + x);
                    if (new float[] { Owner.Border.RadiusBottomLeft, Owner.Border.RadiusBottomRight, Owner.Border.RadiusTopLeft, Owner.Border.RadiusTopRight }.Sum() != 0)
                    {
                        if ((x >= topLeft.StartX && x <= topLeft.EndX) && (y >= topLeft.StartY && y <= topLeft.EndY))
                            colors[index] = Colors.Transparent;
                        else if ((x >= topRight.StartX && x <= topRight.EndX) && (y >= topRight.StartY && y <= topRight.EndY))
                            colors[index] = Colors.Transparent;
                        else if ((x >= bottomLeft.StartX && x <= bottomLeft.EndX) && (y >= bottomLeft.StartY && y <= bottomLeft.EndY))
                            colors[index] = Colors.Transparent;
                        else if ((x >= bottomRight.StartX && x <= bottomRight.EndX) && (y >= bottomRight.StartY && y <= bottomRight.EndY))
                            colors[index] = Colors.Transparent;
                        else
                            colors[index] = Color;
                    }
                    else
                        colors[index] = Color;
                }

            await DrawCorners(colors, width, height);

            colors = GaussianBlur.Blur(colors, width, height, BlurRadius);

            return await SaveAsPng(width, height, colors);
        }

        Task<Rec> GetCorner(int increaseValue, int width, int height, int corner)
        {
            Rec resutl = null;
            switch (corner)
            {
                case 0:
                    //LeftTop
                    resutl = new Rec
                    {
                        StartX = SHADOW_MARGIN,
                        EndX = increaseValue + SHADOW_MARGIN + (int)Owner.Border.RadiusTopLeft / 2,
                        StartY = SHADOW_MARGIN,
                        EndY = increaseValue + SHADOW_MARGIN + (int)Owner.Border.RadiusTopLeft / 2
                    };
                    break;
                case 1:
                    //RightTop
                    resutl = new Rec
                    {
                        StartX = (width - SHADOW_MARGIN) - increaseValue - (int)Owner.Border.RadiusTopRight / 2,
                        EndX = width,
                        StartY = SHADOW_MARGIN,
                        EndY = increaseValue + SHADOW_MARGIN + (int)Owner.Border.RadiusTopRight / 2
                    };
                    break;
                case 2:
                    //LeftBottom
                    resutl = new Rec
                    {
                        StartX = SHADOW_MARGIN,
                        EndX = increaseValue + SHADOW_MARGIN + (int)Owner.Border.RadiusBottomLeft / 2,
                        StartY = (height - SHADOW_MARGIN) - increaseValue - (int)Owner.Border.RadiusBottomLeft / 2,
                        EndY = height - SHADOW_MARGIN
                    };
                    break;
                case 3:
                    //RightBottom
                    resutl = new Rec
                    {
                        StartX = (width - SHADOW_MARGIN) - increaseValue - (int)Owner.Border.RadiusBottomRight / 2,
                        EndX = width - SHADOW_MARGIN,
                        StartY = (height - SHADOW_MARGIN) - increaseValue - (int)Owner.Border.RadiusBottomRight / 2,
                        EndY = height - SHADOW_MARGIN
                    };
                    break;
                default: break;
            }

            return Task.FromResult(resutl);
        }

        Task DrawCorners(Color[] colors, int width, int height)
        {
            var borderRadius = new float[] { Owner.Border.RadiusBottomLeft, Owner.Border.RadiusBottomRight, Owner.Border.RadiusTopLeft, Owner.Border.RadiusTopRight };
            if (borderRadius.Sum() != 0)
            {
                var stroke = IncreaseValue + (int)Owner.Border.RadiusTopLeft;
                var ownerWidth = Owner.Width.CurrentValue;
                var ownerHeight = Owner.Height.CurrentValue;

                if (borderRadius.Distinct().IsSingle() && ownerWidth.AlmostEquals(ownerHeight) && (int)borderRadius.First() == ownerWidth / 2)
                {
                    DrawCircle(colors, stroke, width, height / 2);
                }
                else
                {
                    DrawCustomArc(colors, width, 0, TOP_LEFT);
                    DrawCustomArc(colors, width, 0, TOP_RIGHT);
                    DrawCustomArc(colors, width, height, BOTTOM_RIGHT);
                    DrawCustomArc(colors, width, height, BOTTOM_LEFT);
                }
            }

            return Task.CompletedTask;
        }

        Task DrawCustomArc(Color[] source, int width, int height, int cornerPosition)
        {
            double radius;
            int stroke;
            var lengthSubMargin = (width - SHADOW_MARGIN) * (height - SHADOW_MARGIN);

            switch (cornerPosition)
            {
                case TOP_LEFT:
                    radius = Owner.Border.RadiusTopLeft;
                    stroke = IncreaseValue + (int)Owner.Border.RadiusTopLeft;
                    break;
                case TOP_RIGHT:
                    radius = Owner.Border.RadiusTopRight;
                    stroke = IncreaseValue + (int)Owner.Border.RadiusTopRight;
                    break;
                case BOTTOM_RIGHT:
                    radius = Owner.Border.RadiusBottomRight;
                    stroke = IncreaseValue + (int)Owner.Border.RadiusBottomRight - 1;
                    break;
                default:
                    radius = Owner.Border.RadiusBottomLeft;
                    stroke = IncreaseValue + (int)Owner.Border.RadiusBottomLeft;
                    break;
            }

            var cornerAdjustingValue = SHADOW_MARGIN * 2 + radius;
            int xPos = 0, yPos = 0;
            double circles = 1;
            for (int j = 1; j < stroke; j++)
            {
                circles += 1;
                for (var i = 0.0; i < FULL_CIRCLE_DEGREE; i += 0.1)
                {
                    var angle = i * Math.PI / HALF_CIRCLE_DEGREE;
                    switch (cornerPosition)
                    {
                        case TOP_LEFT:
                            xPos = (int)(cornerAdjustingValue + circles * Math.Cos(angle));
                            yPos = (int)(cornerAdjustingValue + circles * Math.Sin(angle));
                            break;
                        case TOP_RIGHT:
                            var xPosLineLen = width - cornerAdjustingValue;
                            xPos = (int)(xPosLineLen + circles * Math.Cos(angle));
                            yPos = (int)(YOffset + cornerAdjustingValue + circles * Math.Sin(angle));
                            break;
                        case BOTTOM_RIGHT:
                            xPos = (int)(Math.Abs(width - cornerAdjustingValue + 1) + circles * Math.Cos(angle));
                            yPos = (int)(Math.Abs(height - cornerAdjustingValue + 1) + circles * Math.Sin(angle));
                            break;
                        default:
                            var yPosLineLen = height - cornerAdjustingValue;
                            xPos = (int)(cornerAdjustingValue + circles * Math.Cos(angle));
                            yPos = (int)(yPosLineLen + circles * Math.Sin(angle));
                            break;
                    }

                    var index = Math.Abs(yPos * width + xPos);
                    if (index > source.Length) continue;
                    source[index] = Color;
                }
            }
            return Task.CompletedTask;
        }

        Task DrawCircle(Color[] source, int stroke, int width, int center)
        {
            var radius = Owner.Border.RadiusTopLeft;
            int xPos = 0, yPos = 0;
            double circles = 1;
            for (int j = 1; j < stroke; j++)
            {
                circles += 1;
                for (var i = 0.0; i < FULL_CIRCLE_DEGREE; i += 0.1)
                {
                    var angle = i * Math.PI / HALF_CIRCLE_DEGREE;
                    xPos = (int)(center + circles * Math.Cos(angle));
                    yPos = (int)(center + circles * Math.Sin(angle));

                    var index = Math.Abs(yPos * width + xPos);
                    source[index] = Color;
                }
            }
            return Task.CompletedTask;
        }
    }

    internal class Rec
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
    }
}