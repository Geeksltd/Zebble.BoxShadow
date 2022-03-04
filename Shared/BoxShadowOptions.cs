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
            if (ShouldBorderOverride) return Owner.BorderRadius.Get(br => $"b{br.TopLeft},{br.TopRight},{br.BottomRight},{br.BottomLeft}");

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
                    Owner.BorderRadius.TopLeft,
                    Owner.BorderRadius.TopRight,
                    Owner.BorderRadius.BottomRight,
                    Owner.BorderRadius.BottomLeft
                };

                return borderRadius;
            }

            return null;
        }
    }
}
