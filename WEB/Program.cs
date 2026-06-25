
global using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WEB;
using WEB.Services.Config;
using WEB.Services.Inventory;
using WEB.Services.Products;
using WEB.Services.Auth;
using WEB.Services.LocalStorage;
using WEB.Services.Upload;

var server_local = "http://localhost:5220/";
//var Deploy_Server = "https://scanapi.dpdns.org:443";

// Alternativa: si la API está en otro puerto, usa:
// var ritrama_local = "https://localhost:7000/"; // Para HTTPS en desarrollo

//var ritrama_cloud = "...";

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddHttpClient("ritrama", options => {
    options.BaseAddress = new Uri(server_local);
    options.Timeout = TimeSpan.FromSeconds(15);
    options.DefaultRequestHeaders.Add("User-Agent", "BlazorApp");
});

builder.Services.AddBlazorBootstrap();

//Inyectar los servcios de la aplicacion.
builder.Services.AddScoped<IInventoryServices, InventoryServices>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILocalStorage, LocalStorage>();
builder.Services.AddScoped<UploadService>();

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();


await builder.Build().RunAsync();
