using System.Threading.Tasks;

namespace Zebble
{
    public static class BoxShadowExtensions
    {
        public static void BoxShadow(this View owner, int xOffset = 0, int yOffset = 0, int blurRadius = 7, int expand = -5,
            Color color = null)
        {
            owner.WhenShown(async () =>
            {
                if (owner.Id == null) throw new System.Exception("The owner of shadow should have unique identification");

                var id = $"{owner.Id}BoxShadow";
                if (Nav.CurrentPage.AllChildren.Any(rec => rec.Id == id)) return; // Already added

                var shadow = new BoxShadow
                {
                    For = owner,
                    BlurRadius = blurRadius,
                    Expand = expand,
                    XOffset = xOffset,
                    YOffset = yOffset,
                    Color = color ?? Colors.DarkGray,
                    Id = id,
                    ZIndex = owner.ZIndex - 1
                };

                if (owner.Parent is Canvas canvas) canvas.ClipChildren = false;
                await shadow.Draw();
                await owner.Parent.AddBefore(owner, shadow);
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