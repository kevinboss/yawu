using Microsoft.JSInterop;

namespace WebClient;

public class JsConsole
{
    private readonly IJSRuntime _jsRuntime;

    public JsConsole(IJSRuntime jSRuntime)
    {
        _jsRuntime = jSRuntime;
    }

    public async Task LogAsync(string message)
    {
        await _jsRuntime.InvokeVoidAsync("console.log", message);
    }
}