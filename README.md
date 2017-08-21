# Zebble.Shadow
A UI component for Zebble apps that can be added to another object to draw a shadow behind it.

It supports the common options from the CSS box-shadow attribute including Color, X-Offset, Y-Offset, Blur, and Spread.

## How does it work?

Shadow support in native mobile platforms is limited due to performance and other reasons. To solve that, we use this technique:

For every unique shadow settings combination (color, width, height, blur and spread) it will generate the shadow image as a PNG file once.
This is done only the first time, and the resulting image will be saved in the cache folder and reused for all future cases where it needs to be rendered.

The decoded image will also be cached in memory in the standard way that Zebble apps do it.

### Performance?

Usually in apps, a small number of shadow combinations will be used.
So with this technique, we only add a one-off performance impact for the first time a shadow is requestd.
After that in terms of performance, drawing a shadow will be effectively as simple as rendering an image.
