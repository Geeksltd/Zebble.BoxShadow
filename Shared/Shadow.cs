namespace Zebble
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class Shadow : Canvas
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
            Owner.OpacityChanged.Handle(SyncWithOwner);
            Owner.VisibleChanged.Handle(SyncWithOwner);
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
        }
    }
}