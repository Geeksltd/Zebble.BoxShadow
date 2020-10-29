namespace Zebble
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using Windows.Graphics.Imaging;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Media.Imaging;

    public partial class BoxShadow
    {
        public override async Task OnInitialized()
        {
            await WhenShown(() =>
            {
                Thread.UI.Run(() =>
                {
                    var native = this.Native();
                    native.IsHitTestVisible = false;
                });
            });
        }
    }
}