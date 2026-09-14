using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HalalChain.Marketplace.Realtime;

public sealed class LocalizerBootstrap : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await JS.InvokeVoidAsync("hcBootstrap.init");
    }
}
