using System;

namespace Zebble
{
    public class BoxShadowOptions
    {
        bool ShouldBorderOverride;
        bool ShouldPositionOverride;

        public static BoxShadowOptions Override(View viewToOverride, bool border, bool position)
            => new BoxShadowOptions
            {
                ShouldBorderOverride = border,
                ShouldPositionOverride = position,
                Owner = viewToOverride
            };

        public View Owner { get; set; }

        public string GetBorderString()
        {
            if (!ShouldBorderOverride) return null;

            return Owner.BorderRadius.Get(br => $"b{br.TopLeft},{br.TopRight},{br.BottomRight},{br.BottomLeft}");
        }

        public string GetPositionString()
        {
            if (!ShouldPositionOverride) return null;

            var margin = Owner.Margin;
            var padding = Owner.Parent.Padding;

            return $"p{margin.Top.CurrentValue},{margin.Left.CurrentValue},{padding.Top.CurrentValue},{padding.Left.CurrentValue}";
        }

        public float[] GetBorderRadius()
        {
            if (!ShouldBorderOverride) return null;

            return new float[] {
                Owner.BorderRadius.TopLeft,
                Owner.BorderRadius.TopRight,
                Owner.BorderRadius.BottomRight,
                Owner.BorderRadius.BottomLeft
            };
        }
    }
}
