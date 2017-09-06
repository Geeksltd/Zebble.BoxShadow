namespace Zebble
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Zebble.Plugin;

    public partial class Shadow : ImageView
    {
        View Owner;

        public Shadow()
        {
            Absolute = true;
            Nav.CurrentPage.Add(this);
        }
        public View For
        {
            get => Owner;
            set
            {
                if (Owner == value) return;
                if (Owner != null) throw new Exception("Shadow.For cannot be changed once it's set.");

                Owner = value;
            }
        }

        public Color Color { get; set; } = Colors.Black;
        public int XOffset { get; set; } = 0;
        public int YOffset { get; set; } = 0;
        public int SpreadRadius { get; set; } = 0; //{ get; set {  Owner.X.Set( value); Owner.Y.Set(value); } } = 0;
        public int BlurRadius { get; set; } = 10;

        public void SetSpreadRadius(int value)
        {
            X.Set(X.CurrentValue + value / 2);
            Y.Set(Y.CurrentValue + value / 2);
            Height.Set(Owner.Height.CurrentValue + value);
            Width.Set(Owner.Width.CurrentValue + value);
            SpreadRadius = value;
        }
        public override async Task OnPreRender()
        {
            await base.OnPreRender();

            if (Owner == null) throw new Exception("'For' should be specified for a Shadow.");

            // Attach to the owner:
            SyncWithOwner();
            Owner.X.Changed.HandleWith(SyncWithOwner);
            Owner.Y.Changed.HandleWith(SyncWithOwner);
            // Owner.OpacityChanged.Handle(SyncWithOwner);
            // Owner.VisibleChanged.Handle(SyncWithOwner);
            //BackgroundImageStretch = Stretch.Fill;



            // TODO: Upon removal of the owner, remove this too. Also set its visibility

            var image = GetImagePath();
            //if (!await image.SyncExists())
            await CreateImageFile(image);
            Path = image.FullName;
        }

        void SyncWithOwner()
        {
            var increaseValue = BlurRadius + SpreadRadius * 2;
            Absolute = true;

            X.Set(Owner.ActualX - increaseValue / 2);
            Y.Set(Owner.ActualY - increaseValue / 2);

            //  Height.Set(Owner.Height.CurrentValue + (BlurRadius * 2) + (SpreadRadius * 2));
            //  Width.Set(Owner.Width.CurrentValue + (BlurRadius * 2) + (SpreadRadius * 2));
            Height.Set(Owner.Height.CurrentValue + increaseValue + YOffset);
            Width.Set(Owner.Width.CurrentValue + increaseValue + XOffset);


            Visible = Owner.Visible;
            Opacity = Owner.Opacity;
        }
        FileInfo GetImagePath()
        {
            var name = new object[] { Owner.Width, Owner.Height, Color, SpreadRadius, BlurRadius }
             .ToString("|").ToIOSafeHash();

            return Device.IO.Cache.GetFile(name + ".png");
        }

        async Task CreateImageFile(FileInfo savePath)
        {
            // TODO: Generate an image for the blur using semi transparent pixels:
            var increaseValue = BlurRadius + SpreadRadius * 2;
            var height = (int)Height.CurrentValue;
            var width = (int)Width.CurrentValue;


            var length = height * width;
            Color[] colors = Enumerable.Repeat(Colors.Transparent, length).ToArray(); ;// new Color[length];


            double alphaRatio = BlurRadius == 0 ? 0 : Math.Abs(Color.Alpha - BackgroundColor.Alpha) / (double)(BlurRadius * 1.2);
            var radius = BlurRadius == 0 ? 0 : (BlurRadius / 3);

            for (var y = YOffset; y < height; y++)
                for (var x = XOffset; x < width; x++)
                {
                    var isCorner = true;
                    int i = y * width + x;
                    byte alpha = Convert.ToByte(width / 2 - Math.Abs(width / 2 - x));

                    if (x % width < radius) //left
                    {
                        if (y < x) // Top left band
                            alpha = Convert.ToByte(y * alphaRatio);
                        else if ((height - y) < x) // bottom left band
                            alpha = Convert.ToByte((height - y) * alphaRatio);
                        else
                            alpha = Convert.ToByte(x * alphaRatio);
                    }
                    else if (x % width >= width - radius) //right
                    {
                        if (y < (width - x)) // Top right band
                            alpha = Convert.ToByte(y * alphaRatio);
                        else if ((height - y) < (width - x)) // Bottom right band
                            alpha = Convert.ToByte((height - y) * alphaRatio);
                        else
                            alpha = Convert.ToByte((width - x) * alphaRatio);
                    }
                    else if (y < radius) // Top band
                        alpha = Convert.ToByte(y * alphaRatio);
                    else if (y >= (height - 1 - radius)) // Bottom band
                        alpha = Convert.ToByte((height - y) * alphaRatio);
                    else if (y >= increaseValue && y <= (height - increaseValue - YOffset - 1)) // crop the center
                    {
                        if (x >= increaseValue && x <= (width - increaseValue - XOffset - 1))
                            alpha = 255;
                        else
                            isCorner = false;
                    }
                    else   //others
                        isCorner = false;

                    if (isCorner)
                        colors[i] = Colors.Transparent;
                    else
                        colors[i] = new Zebble.Color(Color.Red, Color.Green, Color.Blue, Color.Alpha);// Convert.ToByte(Math.Abs(Color.Alpha - 10)));
                }

            // Blur it           
            colors = GaussianBlur.Blur(colors, width, height, BlurRadius, increaseValue, XOffset, YOffset);

            await SaveAsPng(savePath, width, height, colors);
        }
    }
}