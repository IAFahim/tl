using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Playground;

if (Environment.GetEnvironmentVariable("TL_PLAYGROUND_SMOKE") == "1")
{
    Console.Write(Play.SmokeRun.Launch());
    Console.Write(Play.LiveAuthoring.Receipt());
    return;
}

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();
