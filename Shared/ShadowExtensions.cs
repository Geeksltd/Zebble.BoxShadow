using System;
using System.Collections.Generic;
using System.Text;

namespace Zebble
{
    public static class ShadowExtensions
    {
        public static void Shadow(this View item, int xOffset, int yOffset, int blurRadius, int spreadRadius, Color color)
        {
            Shadow shadow = new Shadow();
            shadow.For = item;
            shadow.BlurRadius = blurRadius;
            shadow.SpreadRadius = spreadRadius;
            shadow.XOffset = xOffset;
            shadow.YOffset = yOffset;
            shadow.Color = color;
            item.ZIndex = 1;
            shadow.ZIndex = 0;

            Nav.CurrentPage.Add(shadow);
          //  item.Parent.AddBefore(item, shadow);
        }
    }
}
