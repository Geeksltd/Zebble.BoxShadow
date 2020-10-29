using System;

namespace Zebble
{
    public class BoxShadowOptions
    {
        bool ShouldBorderOverride, ShouldPositionOverride;

        public static BoxShadowOptions Override(View viewToOverride, bool border, bool position)
        {
            return new BoxShadowOptions { ShouldBorderOverride = border, ShouldPositionOverride = position, Owner = viewToOverride };
        }

        public View Owner { get; set; }

        public string GetBorderString()
        {
            if (ShouldBorderOverride) return Owner.Border.Get(b => $"b{b.RadiusTopLeft},{b.RadiusTopRight},{b.RadiusBottomRight},{b.RadiusBottomLeft}");

            return null;
        }

        public string GetPositionString()
        {
            if (ShouldPositionOverride)
            {
                var margin = Owner.Margin;
                var padding = Owner.Parent.Padding;

                return $"p{margin.Top.CurrentValue},{margin.Left.CurrentValue},{padding.Top.CurrentValue},{padding.Left.CurrentValue}";
            }

            return null;
        }

        public float[] GetBorderRadius()
        {
            if (ShouldBorderOverride)
            {
                var borderRadius = new float[] {
                    Owner.Border.RadiusTopLeft,
                    Owner.Border.RadiusTopRight,
                    Owner.Border.RadiusBottomRight,
                    Owner.Border.RadiusBottomLeft
                };

                return borderRadius;
            }

            return null;
        }
    }
}
