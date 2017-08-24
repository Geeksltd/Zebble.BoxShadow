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
            var imageBuffer = new byte[Convert.ToInt32(4 * Width.CurrentValue * Height.CurrentValue)];


            for (int row = 0; row < Owner.Height.CurrentValue; row++)
            {
                for (int col = 0; col < Owner.Width.CurrentValue; col++)
                {
                    var offset = (row * (int)100 * 4) + (col * 4);
                    imageBuffer[offset] = 0x00;      // Red
                    imageBuffer[offset + 1] = 0xFF;  // Green
                    imageBuffer[offset + 2] = 0x00;  // Blue
                    imageBuffer[offset + 3] = 0xFF;  // Alpha
                }
            }

            var result = Save(savePath, imageBuffer);

            return Task.CompletedTask;
        }




        //const int CHANNELS = 4;

        //public static byte[] CreateShadow(byte[] bitmap, int width,int height, int radius, float opacity)
        //{
        //    // Alpha mask with opacity
        //    //var matrix = new ColorMatrix(new float[][] {
        //    //new float[] {  0F,  0F,  0F, 0F,      0F },
        //    //new float[] {  0F,  0F,  0F, 0F,      0F },
        //    //new float[] {  0F,  0F,  0F, 0F,      0F },
        //    //new float[] { -1F, -1F, -1F, opacity, 0F },
        //    //new float[] {  1F,  1F,  1F, 0F,      1F }
        //    //   });
        //    //  var imageAttributes = new ImageAttributes();
        //    //  imageAttributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);



        //    var shadow = new byte[width + 4 * radius* height + 4 * radius];

        //    Rectangle r = new Rectangle();


        //    using (var graphics = Graphics.FromImage(shadow))
        //        graphics.DrawImage(bitmap, new Rectangle(2 * radius, 2 * radius, width, height), 0, 0, width, height, GraphicsUnit.Pixel, imageAttributes);

        //    // Gaussian blur
        //    var clone = shadow.Clone() as Bitmap;
        //    var shadowData = shadow.LockBits(new Rectangle(0, 0, shadow.Width, shadow.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        //    var cloneData = clone.LockBits(new Rectangle(0, 0, clone.Width, clone.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        //    var boxes = DetermineBoxes(radius, 3);
        //    BoxBlur(shadowData, cloneData, shadow.Width, shadow.Height, (boxes[0] - 1) / 2);
        //    BoxBlur(shadowData, cloneData, shadow.Width, shadow.Height, (boxes[1] - 1) / 2);
        //    BoxBlur(shadowData, cloneData, shadow.Width, shadow.Height, (boxes[2] - 1) / 2);

        //   // shadow.UnlockBits(shadowData);
        //   // clone.UnlockBits(cloneData);
        //    return shadow;
        //}

        //private static void BoxBlur(byte[] p1, byte[] p2, int width, int height, int radius)
        //{
        //    int radius2 = 2 * radius + 1;
        //    int[] sum = new int[CHANNELS];
        //    int[] FirstValue = new int[CHANNELS];
        //    int[] LastValue = new int[CHANNELS];

        //    // Horizontal
        //    int stride = width * 4;
        //    for (var row = 0; row < height; row++)
        //    {
        //        int start = row * stride;
        //        int left = start;
        //        int right = start + radius * CHANNELS;

        //        for (int channel = 0; channel < CHANNELS; channel++)
        //        {
        //            FirstValue[channel] = p1[start + channel];
        //            LastValue[channel] = p1[start + (width - 1) * CHANNELS + channel];
        //            sum[channel] = (radius + 1) * FirstValue[channel];
        //        }
        //        for (var column = 0; column < radius; column++)
        //            for (int channel = 0; channel < CHANNELS; channel++)
        //                sum[channel] += p1[start + column * CHANNELS + channel];
        //        for (var column = 0; column <= radius; column++, right += CHANNELS, start += CHANNELS)
        //            for (int channel = 0; channel < CHANNELS; channel++)
        //            {
        //                sum[channel] += p1[right + channel] - FirstValue[channel];
        //                p2[start + channel] = (byte)(sum[channel] / radius2);
        //            }
        //        for (var column = radius + 1; column < width - radius; column++, left += CHANNELS, right += CHANNELS, start += CHANNELS)
        //            for (int channel = 0; channel < CHANNELS; channel++)
        //            {
        //                sum[channel] += p1[right + channel] - p1[left + channel];
        //                p2[start + channel] = (byte)(sum[channel] / radius2);
        //            }
        //        for (var column = width - radius; column < width; column++, left += CHANNELS, start += CHANNELS)
        //            for (int channel = 0; channel < CHANNELS; channel++)
        //            {
        //                sum[channel] += LastValue[channel] - p1[left + channel];
        //                p2[start + channel] = (byte)(sum[channel] / radius2);
        //            }
        //    }

        //    // Vertical
        //    stride = width * 4;
        //    for (int column = 0; column < width; column++)
        //    {
        //        int start = column * CHANNELS;
        //        int top = start;
        //        int bottom = start + radius * stride;

        //        for (int channel = 0; channel < CHANNELS; channel++)
        //        {
        //            FirstValue[channel] = p2[start + channel];
        //            LastValue[channel] = p2[start + (height - 1) * stride + channel];
        //            sum[channel] = (radius + 1) * FirstValue[channel];
        //        }
        //        for (int row = 0; row < radius; row++)
        //            for (int channel = 0; channel < CHANNELS; channel++)
        //                sum[channel] += p2[start + row * stride + channel];
        //        for (int row = 0; row <= radius; row++, bottom += stride, start += stride)
        //            for (int channel = 0; channel < CHANNELS; channel++)
        //            {
        //                sum[channel] += p2[bottom + channel] - FirstValue[channel];
        //                p1[start + channel] = (byte)(sum[channel] / radius2);
        //            }
        //        for (int row = radius + 1; row < height - radius; row++, top += stride, bottom += stride, start += stride)
        //            for (int channel = 0; channel < CHANNELS; channel++)
        //            {
        //                sum[channel] += p2[bottom + channel] - p2[top + channel];
        //                p1[start + channel] = (byte)(sum[channel] / radius2);
        //            }
        //        for (int row = height - radius; row < height; row++, top += stride, start += stride)
        //            for (int channel = 0; channel < CHANNELS; channel++)
        //            {
        //                sum[channel] += LastValue[channel] - p2[top + channel];
        //                p1[start + channel] = (byte)(sum[channel] / radius2);
        //            }
        //    }
        //}

        //private static int[] DetermineBoxes(double Sigma, int BoxCount)
        //{
        //    double IdealWidth = Math.Sqrt((12 * Sigma * Sigma / BoxCount) + 1);
        //    int Lower = (int)Math.Floor(IdealWidth);
        //    if (Lower % 2 == 0)
        //        Lower--;
        //    int Upper = Lower + 2;

        //    double MedianWidth = (12 * Sigma * Sigma - BoxCount * Lower * Lower - 4 * BoxCount * Lower - 3 * BoxCount) / (-4 * Lower - 4);
        //    int Median = (int)Math.Round(MedianWidth);

        //    int[] BoxSizes = new int[BoxCount];
        //    for (int i = 0; i < BoxCount; i++)
        //        BoxSizes[i] = (i < Median) ? Lower : Upper;
        //    return BoxSizes;
        //}

    }
}