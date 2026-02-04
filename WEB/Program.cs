using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WEB;
using WEB.Services.Inventory;

var ritrama_local = "http://localhost:5220/";
//var ritrama_cloud = "...";

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddHttpClient("ritrama", options => {
    options.BaseAddress = new Uri(ritrama_local);
    options.Timeout = TimeSpan.FromSeconds(15);
    options.DefaultRequestHeaders.Add("User-Agent", "BlazorApp");
});

builder.Services.AddBlazorBootstrap();


//Inyectar los servcios de la aplicacion.
builder.Services.AddScoped<IInventoryServices, InventoryServices>();

await builder.Build().RunAsync();
