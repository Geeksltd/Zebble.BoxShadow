namespace Zebble
{
    using System;
    using System.IO;
    using System.Threading.Tasks;


    public partial class Shadow : Canvas
    {
        View Owner;

        public Shadow() => Absolute = true;

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
        public int XOffset { get; set; } = 10;
        public int YOffset { get; set; } = 10;
        public int SpreadRadius { get; set; } = 10;
        public int BlurRadius { get; set; } = 10;

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
            await Owner.Parent.AddBefore(Owner);

            // TODO: Upon removal of the owner, remove this too. Also set its visibility

            var image = GetImagePath();
            if (!await image.SyncExists()) await CreateImageFile(image);
            BackgroundImagePath = image.FullName;
        }

        void SyncWithOwner()
        {
            X.Set(Owner.ActualX + XOffset);
            Y.Set(Owner.ActualY + YOffset);
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
            var height = (int)Owner.Height.CurrentValue;
            var width = (int)Owner.Width.CurrentValue;
            var length = height * width;
            Color[] colors = new Color[length];
            Color backgroundColor = Colors.Black;


            int rMax = backgroundColor.Red;
            int rMin = Color.Red;

            int gMax = backgroundColor.Green;
            int gMin = Color.Green;

            int bMax = backgroundColor.Blue;
            int bMin = Color.Blue;

            int yRadian = width / BlurRadius;
            int xRadian = height / BlurRadius;

            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if ((y < BlurRadius)) // Top band
                    {
                        int density = (yRadian * BlurRadius) - (y * yRadian);
                        byte rAverage = (byte)(rMin + ((rMax - rMin) * density / width));
                        var gAverage = (byte)(gMin + (byte)((gMax - gMin) * density / width));
                        var bAverage = (byte)(bMin + (byte)((bMax - bMin) * density / width));
                        colors[i] = new Color(rAverage, gAverage, bAverage);
                    }
                    else if (y >= height - 1 - BlurRadius)  // Bottom band
                    {
                        int density = (y + 1 + yRadian - height) * yRadian;
                        byte rAverage = (byte)(rMin + ((rMax - rMin) * density / width));
                        var gAverage = (byte)(gMin + (byte)((gMax - gMin) * density / width));
                        var bAverage = (byte)(bMin + (byte)((bMax - bMin) * density / width));
                        colors[i] = new Color(rAverage, gAverage, bAverage);
                    }
                    else if (x % width < BlurRadius) // Left band
                    {
                        // colors[i] = new Color(backgroundColor.Red, backgroundColor.Green, backgroundColor.Blue, color.Alpha);
                        int density = (xRadian * BlurRadius) - (x * yRadian);
                        byte rAverage = (byte)(rMin + ((rMax - rMin) * density / height));
                        var gAverage = (byte)(gMin + (byte)((gMax - gMin) * density / height));
                        var bAverage = (byte)(bMin + (byte)((bMax - bMin) * density / height));
                        colors[i] = new Color(rAverage, gAverage, bAverage);
                    }
                    else if (x % width >= width - BlurRadius) // right band
                    {
                        //  colors[i] = new Color(backgroundColor.Red, backgroundColor.Green, backgroundColor.Blue, color.Alpha);
                        int density = (x + 1 + xRadian - width) * yRadian;
                        byte rAverage = (byte)(rMin + ((rMax - rMin) * density / height));
                        var gAverage = (byte)(gMin + (byte)((gMax - gMin) * density / height));
                        var bAverage = (byte)(bMin + (byte)((bMax - bMin) * density / height));
                        colors[i] = new Color(rAverage, gAverage, bAverage);
                    }
                    else //center
                    {
                        colors[i] = new Color(Color.Red, Color.Green, Color.Blue, Color.Alpha);
                    }
                }

            var result = SaveAsPng(savePath, width, height, BlurRadius, colors);
            return Task.CompletedTask;
        }
    }
}