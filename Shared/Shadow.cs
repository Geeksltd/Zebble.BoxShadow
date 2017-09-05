namespace Zebble
{
    using System;
    using System.IO;
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
            if (!await image.SyncExists())
                await CreateImageFile(image);
            Path = image.FullName;
        }

        void SyncWithOwner()
        {
            var increaseValue = BlurRadius + SpreadRadius;
            Absolute = true;

            X.Set(Owner.ActualX + XOffset - increaseValue / 2);
            Y.Set(Owner.ActualY + YOffset - increaseValue / 2);

            //  Height.Set(Owner.Height.CurrentValue + (BlurRadius * 2) + (SpreadRadius * 2));
            //  Width.Set(Owner.Width.CurrentValue + (BlurRadius * 2) + (SpreadRadius * 2));
            Height.Set(Owner.Height.CurrentValue + increaseValue);
            Width.Set(Owner.Width.CurrentValue + increaseValue);


            Visible = Owner.Visible;
            Opacity = Owner.Opacity;
        }
        FileInfo GetImagePath()
        {
            var name = new object[] { Owner.Width, Owner.Height, Color, SpreadRadius, BlurRadius }
             .ToString("|").ToIOSafeHash();

            return Device.IO.Cache.GetFile(name + ".png");
        }

        Task CreateImageFile(FileInfo savePath)
        {
            // TODO: Generate an image for the blur using semi transparent pixels:
            var increaseValue = BlurRadius + SpreadRadius;
            var height = (int)Height.CurrentValue;
            var width = (int)Width.CurrentValue;


            var length = height * width;
            Color[] colors = new Color[length];
            Color backgroundColor = Colors.Transparent;

            double alphaRatio = Math.Abs(Color.Alpha - backgroundColor.Alpha) / (double)(BlurRadius * 1.2);
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var isCorner = true;
                    int i = y * width + x;
                    byte alpha = Convert.ToByte(width / 2 - Math.Abs(width / 2 - x));

                    if (x % width < BlurRadius) //left
                    {
                        if (y < x) // Top left band
                            alpha = Convert.ToByte(y * alphaRatio);
                        else if ((height - y) < x) // bottom left band
                            alpha = Convert.ToByte((height - y) * alphaRatio);
                        else
                            alpha = Convert.ToByte(x * alphaRatio);
                    }
                    else if (x % width >= width - BlurRadius) //right
                    {
                        if (y < (width - x)) // Top right band
                            alpha = Convert.ToByte(y * alphaRatio);
                        else if ((height - y) < (width - x)) // Bottom right band
                            alpha = Convert.ToByte((height - y) * alphaRatio);
                        else
                            alpha = Convert.ToByte((width - x) * alphaRatio);
                    }
                    else if (y < BlurRadius) // Top band
                        alpha = Convert.ToByte(y * alphaRatio);
                    else if (y >= (height - 1 - BlurRadius)) // Bottom band
                        alpha = Convert.ToByte((height - y) * alphaRatio);
                    else  //center
                        isCorner = false;

                    if (isCorner)
                        //    colors[i] = new Color(backgroundColor.Red, backgroundColor.Green, backgroundColor.Blue, alpha);
                        colors[i] = new Color(5, 5, 5, alpha);
                    else
                        colors[i] = new Zebble.Color(Color.Red, Color.Green, Color.Blue, Color.Alpha);
                }

            //const int bitsPerPixel = 4;
            //var imageArray = new byte[width * height * bitsPerPixel];

            //for (int i = 0; i < imageArray.Length; i += 4)
            //{
            //    var pixelNumber = i / bitsPerPixel;
            //    var color = colors[pixelNumber].Render();

            //    imageArray[i] = color.B; // Blue
            //    imageArray[i + 1] = color.G;  // Green
            //    imageArray[i + 2] = color.R; // Red
            //    imageArray[i + 3] = color.A;  // Alpha                
            //}

            //// Blur it
            //if (BlurRadius != 0)
            //    imageArray = GaussianBlur.Blur(imageArray, width, height, bitsPerPixel, BlurRadius, increaseValue);
            //var result = SaveAsPng(savePath, width, height, imageArray);

            var result = SaveAsPng(savePath, width, height, BlurRadius, colors, increaseValue);
            return Task.CompletedTask;
        }
    }
}