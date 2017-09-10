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

        public Shadow() => Absolute = true;//  Nav.CurrentPage.Add(this);//   #if UWP//  #endif
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


        public override async Task OnPreRender()
        {

            await base.OnPreRender();

            if (Owner == null) throw new Exception("'For' should be specified for a Shadow.");

            // Attach to the owner:
            await SyncWithOwner();
            Owner.X.Changed.Handle(SyncWithOwner);
            Owner.Y.Changed.Handle(SyncWithOwner);

            Owner.Height.Changed.Handle(SyncWithOwner);
            Owner.Width.Changed.Handle(SyncWithOwner);

            //Owner.Native().
            Owner.VisibilityChanged.Handle(SyncWithOwner);
            // Stretch = Stretch.Fill;
        }


        async Task SyncWithOwner()
        {
            var increaseValue = BlurRadius + SpreadRadius * 2;


            X.Set(Owner.CalculateAbsoluteX() - increaseValue / 2);
            Y.Set(Owner.CalculateAbsoluteY() - increaseValue / 2);

            Height.Set(Owner.Height.CurrentValue + increaseValue + YOffset);
            Width.Set(Owner.Width.CurrentValue + increaseValue + XOffset);


            Visible = Owner.Visible;
            Opacity = Owner.Opacity;

            if (Visible)
            {
                var image = GetImagePath();
                if (!await image.SyncExists())
                    await CreateImageFile(image);
                BackgroundImagePath = image.FullName;

            }
        }
        FileInfo GetImagePath()
        {
            var name = new object[] { Owner.Width.CurrentValue, Owner.Height.CurrentValue, Color, SpreadRadius, BlurRadius }
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


            // double alphaRatio = BlurRadius == 0 ? 0 : Math.Abs(Color.Alpha - BackgroundColor.Alpha) / (double)(BlurRadius * 1.2);
            var radius = BlurRadius == 0 ? 0 : (BlurRadius / 3);

            for (var y = YOffset; y < height; y++)
                for (var x = XOffset; x < width; x++)
                {
                    var isCorner = true;
                    int i = y * width + x;
                    // int alpha = width / 2 - Math.Abs(width / 2 - x);

                    //if (x % width < radius) //left
                    //{
                    //    if (y < x) // Top left band
                    //        alpha = y * alphaRatio;
                    //    else if ((height - y) < x) // bottom left band
                    //        alpha = (height - y) * alphaRatio;
                    //    else
                    //        alpha = x * alphaRatio;
                    //}
                    //else if (x % width >= width - radius) //right
                    //{
                    //    if (y < (width - x)) // Top right band
                    //        alpha = y * alphaRatio;
                    //    else if ((height - y) < (width - x)) // Bottom right band
                    //        alpha = (height - y) * alphaRatio;
                    //    else
                    //        alpha = (width - x) * alphaRatio;
                    //}
                    //else if (y < radius) // Top band
                    //    alpha = y * alphaRatio;
                    //else if (y >= (height - 1 - radius)) // Bottom band
                    //    alpha = (height - y) * alphaRatio;
                    //else if (y >= increaseValue && y <= (height - increaseValue - YOffset - 1)) // crop the center
                    //{
                    //    if (x >= increaseValue && x <= (width - increaseValue - XOffset - 1))
                    //        alpha = 255;
                    //    else
                    //        isCorner = false;
                    //}

                    if (x % width < radius) //left                  
                        isCorner = true;
                    else if (x % width >= width - radius) //right
                        isCorner = true;
                    else if (y < radius) // Top band
                        isCorner = true;
                    else if (y >= (height - 1 - radius)) // Bottom band
                        isCorner = true;
                    else if (y >= increaseValue && y <= (height - increaseValue - YOffset - 1)) // crop the center
                    {
                        if (x >= increaseValue && x <= (width - increaseValue - XOffset - 1))
                            isCorner = true;
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