namespace Zebble
{
    public static class ShadowExtensions
    {
        public static void Shadow(this View owner, int xOffset = 0, int yOffset = 0, int blurRadius = 3, Color color = null)
        {
            owner.WhenShown(() =>
            {
                var id = $"{owner.Id}ShadowBox";
                if (!Nav.CurrentPage.AllChildren.Any(rec => rec.Id == id))
                {
                    var shadow = new Shadow
                    {
                        For = owner,
                        BlurRadius = blurRadius,
                        XOffset = xOffset,
                        YOffset = yOffset,
                        Color = color ?? Colors.Gray,
                        Id = id
                    };

                    shadow.For.Parent.Add(shadow);
                }
            });
        }
    }
}
