namespace Zebble
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    public partial class BoxShadow : BoxShadowCanvas
    {
        const string AlgorithmVersion = "v8";
        const int SHADOW_MARGIN = 11;

        static ConcurrentDictionary<string, AsyncLock> CreationLocks = new ConcurrentDictionary<string, AsyncLock>();
        static List<KeyValuePair<string, byte[]>> RenderedShadows = new List<KeyValuePair<string, byte[]>>();

        View Owner;
        int blurValue;
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

                var border = Owner.BorderRadius.Get(br => $"b{br.TopLeft},{br.TopRight},{br.BottomRight},{br.BottomLeft}");

                var margin = Owner.Margin;
                var padding = Owner.Parent.Padding;

                var position = $"p{margin.Top.CurrentValue},{margin.Left.CurrentValue},{padding.Top.CurrentValue},{padding.Left.CurrentValue}";

                var color = Color.ToString().TrimStart("#");

                if (Options != null)
                {
                    var b = Options.GetBorderString();
                    var p = Options.GetPositionString();

                    if (b != null) border = b;
                    if (p != null) position = p;
                }

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
                if (Owner != null) Log.For(this).Error( "Shadow.For cannot be changed once it's set.");
                else Owner = value;
            }
        }

        public int BlurRadius
        {
            get => blurValue;
            set => blurValue = value / 2;
        }

        public BoxShadowOptions Options { get; set; }

        public static bool ShouldCache { get; set; }

        public int XOffset { get; set; }

        public int YOffset { get; set; }

        public int Expand { get; set; }

        float GetWidth() => Owner.ActualWidth + (BlurRadius + SHADOW_MARGIN + Expand) * 2;

        float GetHeight() => Owner.ActualHeight + (BlurRadius + SHADOW_MARGIN + Expand) * 2;

        int GetBlurValue() => BlurRadius.LimitMax((int)GetWidth() / 2).LimitMax((int)GetHeight() / 2);

        public async Task Draw()
        {
            SyncVisibilities();

            Height.BindTo(Owner.Height, h => GetHeight());
            Width.BindTo(Owner.Width, w => GetWidth());

            X.BindTo(Owner.X, Owner.Margin.Left, Owner.Parent.Padding.Left, (x, margin, containerPadding) =>
               Math.Max(x, margin + containerPadding) + XOffset - (SHADOW_MARGIN + BlurRadius + Owner.Border.Left) - Expand
           );

            Y.BindTo(Owner.Y, Owner.Margin.Top, Owner.Parent.Padding.Top, (y, margin, containerPadding) =>
               Math.Max(y, margin + containerPadding) + YOffset - (SHADOW_MARGIN + BlurRadius + Owner.Border.Top) - Expand
           );

            Owner.Height.Changed.Handle(RenderImage);
            Owner.Width.Changed.Handle(RenderImage);
            Owner.VisibilityChanged.Handle(SyncVisibilities);
            Owner.IgnoredChanged.Handle(SyncVisibilities);
            Owner.OpacityChanged.Handle(SyncVisibilities);
            Owner.ZIndexChanged.Handle(SyncVisibilities);

            await RenderImage();
        }

        void SyncVisibilities()
        {
            Visible = Owner.Visible;
            Ignored = Owner.Ignored;
            Opacity = Owner.Opacity;
            ZIndex = Owner.ZIndex - 1;
        }

        async Task RenderImage()
        {
            if (Owner.ActualWidth == 0 || Owner.ActualHeight == 0) return;

            try
            {
                if (ShouldCache)
                {
                    if (LoadRenderedImage()) return;

                    using (await RenderSyncLock.Lock())
                    {
                        if (!LoadRenderedImage())
                        {
                            var file = await CreateImageFile();
                            var data = file.Exists() ? file.ReadAllBytes() : null;
                            Draw(data, GetBlurValue()).RunInParallel();
                        }
                    }
                }
                else
                {
                    await CreateShadowShapes();
                    Draw(null, GetBlurValue()).RunInParallel();
                }
            }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        bool LoadRenderedImage()
        {
            lock (RenderedShadows)
            {
                var data = RenderedShadows.FirstOrDefault(x => x.Key == CurrentFileName).Value;

                if (data != null)
                {
                    Draw(data, GetBlurValue()).RunInParallel();
                    return true;
                }
            }
            return false;
        }

        async Task<FileInfo> CreateImageFile()
        {
            var file = CurrentFile;

            if (!file.Exists())
            {
                Opacity = 0;

                var creationLock = CreationLocks.GetOrAdd(file.FullName, x => new AsyncLock());

                using (await creationLock.Lock())
                    if (!file.Exists())
                        await DoCreateImageFile(file);

                this.Animate(100.Milliseconds(), x => x.Opacity(Owner.Opacity)).RunInParallel();
            }

            return file;
        }

        async Task DoCreateImageFile(FileInfo file)
        {
            await CreateShadowShapes();

            OnExportCompleted.Handle(async imageData =>
            {
                lock (RenderedShadows)
                    RenderedShadows.Add(new KeyValuePair<string, byte[]>(file.NameWithoutExtension(), imageData));

                await file.WriteAllBytesAsync(imageData);
            });
        }

        async Task CreateShadowShapes()
        {
            float[] borderRadius;

            if (Options == null)
                borderRadius = new float[] {
                    Owner.BorderRadius.TopLeft,
                    Owner.BorderRadius.TopRight,
                    Owner.BorderRadius.BottomRight,
                    Owner.BorderRadius.BottomLeft
                };
            else
                borderRadius = Options.GetBorderRadius();

            if (borderRadius.Sum() != 0)
            {
                var ownerWidth = Owner.Width.CurrentValue;
                var ownerHeight = Owner.Height.CurrentValue;

                if (borderRadius.Distinct().IsSingle() && ownerWidth.AlmostEquals(ownerHeight) && (int)borderRadius[0] == ownerWidth / 2)
                {
                    var stroke = GetStrokeByPosition(CornerPosition.TopLeft);
                    await DrawCircle(stroke);
                }
                else await DrawRect(borderRadius);
            }
            else await DrawRect(borderRadius);
        }

        int GetStrokeByPosition(CornerPosition position)
        {
            switch (position)
            {
                case CornerPosition.TopLeft: return (int)Owner.BorderRadius.TopLeft;
                case CornerPosition.TopRight: return (int)Owner.BorderRadius.TopRight;
                case CornerPosition.BottomRight: return (int)Owner.BorderRadius.BottomRight - 1;
                default: return (int)Owner.BorderRadius.BottomLeft;
            }
        }
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