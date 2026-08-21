global using Microsoft.AspNetCore.Components.Authorization;
global using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WEB;
using WEB.Services.Config;
using WEB.Services.Inventory;
using WEB.Services.Inventario;
using WEB.Services.Products;
using WEB.Services.Auth;
using WEB.Services.CargasIniciales;
using WEB.Services.Customers;
using WEB.Services.Enterprises;
using WEB.Services.LocalStorage;
using WEB.Services.OrdenCompra;
using WEB.Services.Suppliers;
using WEB.Services.Ventas;
using WEB.Services.Almacenes;
using WEB.Services.Vendedores;
using WEB.Services.Versioning;

//var server_local = "http://localhost:5220/";
//var Deploy_Server = "https://scanapi.dpdns.org:443";
var server_etiquetas = "http://192.168.10.10:8080";
//var server_etiquetas = server_local;

// Alternativa: si la API está en otro puerto, usa:
// var ritrama_local = "https://localhost:7000/"; // Para HTTPS en desarrollo

//var ritrama_cloud = "...";

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddHttpClient("ritrama", options => {
    options.BaseAddress = new Uri(server_etiquetas);
    options.Timeout = TimeSpan.FromSeconds(15);
    options.DefaultRequestHeaders.Add("User-Agent", "BlazorApp");
}).AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddBlazorBootstrap();

//Inyectar los servcios de la aplicacion.
builder.Services.AddScoped<IInventoryServices, InventoryServices>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<ICustomersService, CustomersService>();
builder.Services.AddScoped<ISuppliersService, SuppliersService>();
builder.Services.AddScoped<IEnterprisesService, EnterprisesService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILocalStorage>(sp => new LocalStorage(sp.GetRequiredService<IJSRuntime>(), StorageMode.Session));
builder.Services.AddScoped<IOrdenCompraService, OrdenCompraService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<ICargasInicialesService, CargasInicialesService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IAlmacenesService, AlmacenesService>();
builder.Services.AddScoped<IVendedoresService, VendedoresService>();
builder.Services.AddSingleton<AppVersionInfo>();
builder.Services.AddTransient<AuthMessageHandler>();

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();


await builder.Build().RunAsync();
