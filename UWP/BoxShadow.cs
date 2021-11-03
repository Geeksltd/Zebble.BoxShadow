namespace Zebble
{
    using System.Threading.Tasks;

    public partial class BoxShadow
    {
        public override async Task OnInitialized()
        {
            await WhenShown(() =>
            {
                Thread.UI.Run(() =>
                {
                    var native = this.Native();
                    if (native is null) return;
                    native.IsHitTestVisible = false;
                });
            });
        }
    }
}