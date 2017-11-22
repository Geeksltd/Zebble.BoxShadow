namespace Zebble
{
    public static class BoxShadowExtensions
    {
        public static void BoxShadow(this View owner, int xOffset = 0, int yOffset = 0, int blurRadius = 3, Color color = null)
        {
            owner.WhenShown(() =>
            {
                if (owner.Id == null) throw new System.Exception("The owner of shadow should have unique identification");

                var id = $"{owner.Id}BoxShadow";
                if (!Nav.CurrentPage.AllChildren.Any(rec => rec.Id == id))
                {
                    var shadow = new BoxShadow
                    {
                        For = owner,
                        BlurRadius = blurRadius,
                        XOffset = xOffset,
                        YOffset = yOffset,
                        Color = color ?? Colors.Gray,
                        Id = id,
                        ZIndex = owner.ZIndex - 1
                    };

                    shadow.For.Parent.Add(shadow);
                }
            });
        }

        public static byte[] ToByteArray(this Color[] colors, int width, int height)
        {
            const int bitsPerPixel = 4;
            var imageArray = new byte[width * height * bitsPerPixel];

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
