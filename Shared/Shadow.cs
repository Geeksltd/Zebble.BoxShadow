namespace Zebble
{
    public class Shadow
    {
        public static Shadow Default = new Shadow { Color = Color.Parse("#888888"), BlurRadius = 6, Expand = -(3 + 6 / 2) };

        public Shadow() { }

        public Shadow(int xOffset, int yOffset, int blurRadius, int expand, Color color)
        {
            Color = color;
            Expand = expand;
            BlurRadius = blurRadius;
            Y = yOffset;
            X = xOffset;
        }

        public Color Color { get; set; }

        public int Expand { get; set; }

        public int BlurRadius { get; set; }

        public int X { get; set; }

        public int Y { get; set; }
    }
}