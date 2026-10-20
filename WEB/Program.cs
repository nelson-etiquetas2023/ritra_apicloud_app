global using Microsoft.AspNetCore.Components.Authorization;
global using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WEB;
using WEB.Services.Almacenes;
using WEB.Services.Etiquetas;
using WEB.Services.Auth;
using WEB.Services.Inventarios;
using WEB.Services.Config;
using WEB.Services.Customers;
using WEB.Services.Enterprises;
using WEB.Services.Inventario;
using WEB.Services.Inventory;
using WEB.Services.LocalStorage;
using WEB.Services.OrdenCompra;
using WEB.Services.Products;
using WEB.Services.Suppliers;
using WEB.Services.Vendedores;
using WEB.Services.Ventas;
using WEB.Services.Pos;
using WEB.Services.Picking;
using WEB.Services.Turnos;
using WEB.Services.Versioning;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var defaultApiBaseUrl = "http://localhost:5220/";
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? defaultApiBaseUrl;

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddHttpClient("scanpro", options =>
{
    options.BaseAddress = new Uri(apiBaseUrl);
    options.Timeout = TimeSpan.FromSeconds(15);
    options.DefaultRequestHeaders.Add("User-Agent", "BlazorApp");
}).AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddBlazorBootstrap();

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
builder.Services.AddScoped<IInventarioTeoricoService, InventarioTeoricoService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IAlmacenesService, AlmacenesService>();
builder.Services.AddScoped<IEtiquetasService, EtiquetasService>();
builder.Services.AddScoped<IVendedoresService, VendedoresService>();
builder.Services.AddScoped<PosSesionService>();
builder.Services.AddScoped<ITurnosService, TurnosService>();
builder.Services.AddScoped<IPickingService, PickingService>();
builder.Services.AddSingleton<AppVersionInfo>();
builder.Services.AddTransient<AuthMessageHandler>();

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

await builder.Build().RunAsync();
