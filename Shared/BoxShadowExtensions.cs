using System.Threading.Tasks;

namespace Zebble
{
    public static class BoxShadowExtensions
    {
        public static void BoxShadow(this View owner, int xOffset = 0, int yOffset = 0, int blurRadius = 3, int expand = 0, Color color = null)
        {
            BoxShadow shadow = null;

            async Task setVisibility()
            {
                var isOwnerHidden = owner.Opacity == 0 || !owner.Visible || owner.ZIndex < 0 || owner.Ignored;
                if (shadow != null)
                    await shadow.WhenShown(() => shadow.Opacity(isOwnerHidden ? 0 : 1));
            }

            owner.On(x => x.VisibilityChanged, setVisibility)
                .On(x => x.OpacityChanged, setVisibility)
                .On(x => x.ZIndexChanged, setVisibility);

            owner.WhenShown(() =>
            {
                if (owner.Id == null) throw new System.Exception("The owner of shadow should have unique identification");

                var id = $"{owner.Id}BoxShadow";
                if (!Nav.CurrentPage.AllChildren.Any(rec => rec.Id == id))
                {
                    shadow = new BoxShadow
                    {
                        For = owner,
                        BlurRadius = blurRadius,
                        Expand = expand,
                        XOffset = xOffset,
                        YOffset = yOffset,
                        Color = color ?? Colors.Gray,
                        Id = id,
                        ZIndex = owner.ZIndex - 1
                    };

                    shadow.For.Parent.Add(shadow);
                    if (shadow.For.Parent is Canvas)
                        (shadow.For.Parent as Canvas).ClipChildren = false;
                }
            });
        }

        public static byte[] ToByteArray(this Color[] colors, int width, int height)
        {
            const int BITS_PER_PIXEL = 4;
            var imageArray = new byte[width * height * BITS_PER_PIXEL];

            for (var i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                var bytePosition = i * 4;

                imageArray[bytePosition] = color.Red;
                imageArray[bytePosition + 1] = color.Green;
                imageArray[bytePosition + 2] = color.Blue;
                imageArray[bytePosition + 3] = color.Alpha;
            }

            return imageArray;
        }
    }
}