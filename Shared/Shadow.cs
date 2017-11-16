namespace Zebble
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class Shadow : ImageView
    {
        const int SHADOW_MARGIN = 10;
        const int TOP_LEFT = 0, TOP_RIGHT = 1, BOTTOM_RIGHT = 2, BOTTOM_LEFT = 3;
        const double FULL_CIRCLE_DEGREE = 360.0, HALF_CIRCLE_DEGREE = 180.0;

        View Owner;
        bool IsRunning = false;
        DateTime currentTime;

        public Shadow() => Absolute = true;
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
                    var target = GetImagePath();

                    if (!await target.SyncExists())
                    {
                        target.Create().Dispose();
                        currentTime = LocalTime.Now;
                        await CreateImageFile(target);
                        Device.Log.Warning(LocalTime.Now.Subtract(currentTime).TotalSeconds);
                    }
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

        FileInfo GetImagePath()
        {
            var name = new object[] { Owner.Width.CurrentValue, Owner.Height.CurrentValue, Color, BlurRadius }
             .ToString("|").ToIOSafeHash();

            return Device.IO.Cache.GetFile(name + ".png");
        }

        async Task CreateImageFile(FileInfo savePath)
        {
            var increaseValue = BlurRadius * 2;

            var height = (int)Height.CurrentValue;
            var width = (int)Width.CurrentValue;

            var length = height * width;
            var colors = Enumerable.Repeat(Colors.Transparent, length).ToArray();

            var topLeft = await GetCorner(increaseValue, width, height, TOP_LEFT);
            var topRight = await GetCorner(increaseValue, width, height, TOP_RIGHT);
            var bottomLeft = await GetCorner(increaseValue, width, height, BOTTOM_RIGHT);
            var bottomRight = await GetCorner(increaseValue, width, height, BOTTOM_LEFT);

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

            if (new float[] { Owner.Border.RadiusBottomLeft, Owner.Border.RadiusBottomRight, Owner.Border.RadiusTopLeft, Owner.Border.RadiusTopRight }.Sum() != 0)
            {
                await DrawCustomArc(colors, increaseValue, width, 0, TOP_LEFT);
                await DrawCustomArc(colors, increaseValue, width, 0, TOP_RIGHT);
                await DrawCustomArc(colors, increaseValue, width, height, BOTTOM_RIGHT);
                await DrawCustomArc(colors, increaseValue, width, height, BOTTOM_LEFT);
            }

            colors = GaussianBlur.Blur(colors, width, height, BlurRadius);

            await SaveAsPng(savePath, width, height, colors);
        }

        Task<Rec> GetCorner(int increaseValue, int width, int height, int corner)
        {
            Rec resutl = null;
            switch (corner)
            {
                case 0:
                    //LeftTop
                    resutl = new Rec { StartX = SHADOW_MARGIN, EndX = increaseValue + SHADOW_MARGIN, StartY = SHADOW_MARGIN, EndY = increaseValue + SHADOW_MARGIN };
                    break;
                case 1:
                    //RightTop
                    resutl = new Rec { StartX = (width - SHADOW_MARGIN) - increaseValue, EndX = width, StartY = SHADOW_MARGIN, EndY = increaseValue + SHADOW_MARGIN };
                    break;
                case 2:
                    //LeftBottom
                    resutl = new Rec { StartX = SHADOW_MARGIN, EndX = increaseValue + SHADOW_MARGIN, StartY = (height - SHADOW_MARGIN) - increaseValue, EndY = height - SHADOW_MARGIN };
                    break;
                case 3:
                    //RightBottom
                    resutl = new Rec { StartX = (width - SHADOW_MARGIN) - increaseValue, EndX = width - SHADOW_MARGIN, StartY = (height - SHADOW_MARGIN) - increaseValue, EndY = height - SHADOW_MARGIN };
                    break;
                default: break;
            }

            return Task.FromResult(resutl);
        }

        Task DrawCustomArc(Color[] source, int stroke, int width, int height, int cornerPosition)
        {
            double radius;

            if (cornerPosition == TOP_LEFT) radius = Owner.Border.RadiusTopLeft;
            else if (cornerPosition == TOP_RIGHT) radius = Owner.Border.RadiusTopRight;
            else if (cornerPosition == BOTTOM_RIGHT)
            {
                radius = Owner.Border.RadiusBottomRight;
                stroke -= 1;
            }
            else radius = Owner.Border.RadiusBottomLeft;

            var cornerAdjustingValue = SHADOW_MARGIN * 2 + radius;
            int xPos = 0, yPos = 0;
            for (int j = 1; j < stroke; j++)
            {
                radius += 1;
                for (var i = 0.0; i < FULL_CIRCLE_DEGREE; i += 0.1)
                {
                    var angle = i * Math.PI / HALF_CIRCLE_DEGREE;
                    switch (cornerPosition)
                    {
                        case TOP_LEFT:
                            xPos = (int)(cornerAdjustingValue + radius * Math.Cos(angle));
                            yPos = (int)(cornerAdjustingValue + radius * Math.Sin(angle));
                            break;
                        case TOP_RIGHT:
                            var xPosLineLen = width - cornerAdjustingValue;
                            xPos = (int)(xPosLineLen + radius * Math.Cos(angle));
                            yPos = (int)(YOffset + cornerAdjustingValue + radius * Math.Sin(angle));
                            break;
                        case BOTTOM_RIGHT:
                            xPos = (int)(Math.Abs(width - cornerAdjustingValue + 2) + radius * Math.Cos(angle));
                            yPos = (int)(Math.Abs(height - cornerAdjustingValue + 2) + radius * Math.Sin(angle));
                            break;
                        default:
                            var yPosLineLen = height - cornerAdjustingValue;
                            xPos = (int)(cornerAdjustingValue + radius * Math.Cos(angle));
                            yPos = (int)(yPosLineLen + radius * Math.Sin(angle));
                            break;
                    }

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