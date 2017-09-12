using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zebble
{
    public static class ShadowExtensions
    {
        public static void Shadow(this View Owner, int xOffset, int yOffset, int blurRadius, int spreadRadius, Color color)
        {
            Owner.WhenShown(() =>
            {
                var id = $"{Owner.Id}ShadowBox";
                if (!Nav.CurrentPage.AllChildren.Any(rec => rec.Id == id))
                {
                    Shadow shadow = new Shadow();
                    shadow.For = Owner;
                    shadow.BlurRadius = blurRadius;
                    shadow.SpreadRadius = spreadRadius;
                    shadow.XOffset = xOffset;
                    shadow.YOffset = yOffset;
                    shadow.Color = color;
                    // item.ZIndex = 1001;
                    shadow.ZIndex = 101220;
                    shadow.Id = id;

                    //  Nav.CurrentPage.AddAfter(shadow);
                    Nav.CurrentPage.Add(shadow);

                    // Owner.Parent.AddAt(0, shadow, true);
                    //View.Root.AddAt(0, shadow, true);
                    //  Owner.BringToFront().ContinueWith(x => Owner.PushBackToZIndexOrder()).RunInParallel();
                    shadow.BringToFront();
                }
            });
        }
    }
}
