﻿using System.Linq;
using System;
using System.Threading.Tasks;
using Olive;

namespace Zebble
{
    public static class BoxShadowExtensions
    {
        public static void BoxShadow(
            this View owner,
            int xOffset = 0,
            int yOffset = 0,
            int blurRadius = 7,
            int expand = -5,
            Color color = null,
            BoxShadowOptions options = null
        )
        {
            if (owner == null)
            {
                Log.For(typeof(BoxShadowExtensions)).Error("BoxShadow could not work without an owner!");
                return;
            }

            owner.WhenShown(async () =>
            {
                var shadow = new BoxShadow
                {
                    For = owner,
                    Options = options,
                    BlurRadius = blurRadius,
                    Expand = expand,
                    XOffset = xOffset,
                    YOffset = yOffset,
                    Color = color ?? Colors.DarkGray,
                    ZIndex = owner.ZIndex - 1
                };

                owner.GetAllParents().OfType<Canvas>().Do(x => x.ClipChildren = false);
                await shadow.Draw();

                async Task AddToParent()
                {
                    if (owner.Parent.AllChildren.GetElementBefore(owner) is BoxShadow box) await box.RemoveSelf();
                    await owner.Parent.AddBefore(owner, shadow);
                }

                if (owner.Parent != null) await AddToParent();
                else owner.ParentSet.HandleWith(async () => await AddToParent());
            });
        }
    }
}