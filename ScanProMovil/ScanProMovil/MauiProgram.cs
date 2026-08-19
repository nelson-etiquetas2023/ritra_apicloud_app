using Android.Graphics.Drawables;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using ScanProMovil.Data;
using ScanProMovil.Services.Auth;
using ScanProMovil.Services.Compras;
using ScanProMovil.Services.Orders;
using ScanProMovil.Services.Products;
using ScanProMovil.Services.Session;
using ScanProMovil.Services.StockInicial;
using ScanProMovil.ViewModels;
using ScanProMovil.ViewModels.Compras;
using ScanProMovil.ViewModels.Products;
using ScanProMovil.Views;
using ScanProMovil.Views.Compras;
using ScanProMovil.Views.Orders;
using ScanProMovil.Views.Products;
using ScanProMovil.Views.Sincro;
using ScanProMovil.Views.StockInit;

namespace ScanProMovil
{
    public static class MauiProgram
    {

        // Exponemos el ServiceProvider para usarlo en cualquier parte
        public static IServiceProvider? Services { get; private set; }

        public static MauiApp CreateMauiApp()
        {

            var deployLocalApi = "http://192.168.10.10:8080";
            Preferences.Set("ApiBaseUrl", deployLocalApi);
            var builder = MauiApp.CreateBuilder();


            builder.Services.AddSingleton<AuthSession>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<AppSession>();
            builder.Services.AddTransient<AuthHeaderHandler>();

            builder.Services.AddHttpClient("scanpro", options => {
                options.BaseAddress = new Uri(deployLocalApi);
                options.Timeout = TimeSpan.FromSeconds(15);
                options.DefaultRequestHeaders.Add("User-Agent", "MauiApp");
            }).AddHttpMessageHandler<AuthHeaderHandler>();

            //Entity Framework Core - SQLite (CodeFirst + Migraciones).
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "scanpro.db");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath};Foreign Keys=True"),
                ServiceLifetime.Singleton);

            //Injeccion de los VIEWMODELS.
            builder.Services.AddTransient<ProductsViewModel>();
            builder.Services.AddTransient<OrderViewModel>();
            builder.Services.AddTransient<OrderConfigViewModel>();
            builder.Services.AddTransient<SincroOrdersViewModel>();
            builder.Services.AddTransient<ComprasViewModel>();
            builder.Services.AddTransient<SincroComprasViewModels>();
            builder.Services.AddTransient<ProductsLocalViewModels>();   
            builder.Services.AddTransient<AddComprasViewModels>();
            //Injeccion de Pagina XAML.
            builder.Services.AddTransient<GestionProducts>();
            builder.Services.AddTransient<ProductDetails>();
            builder.Services.AddTransient<OrdersPage>();
            builder.Services.AddTransient<ConfigPage>();
            builder.Services.AddTransient<ShoppingPage>();
            builder.Services.AddTransient<SincroOrdersPage>();
            builder.Services.AddTransient<ComprasIndexPage>();
            builder.Services.AddTransient<AddComprasPage>();
            builder.Services.AddTransient<DetailsComprasPage>();
            builder.Services.AddTransient<ConfigComprasPage>();
            builder.Services.AddTransient<SincroComprasPage>();
            builder.Services.AddTransient<LocalDataProductsPage>();
            builder.Services.AddTransient<MainStockInitPage>();
            builder.Services.AddTransient<StockInitDetailPage>();
            builder.Services.AddTransient<CreateStockInitPage>();
            builder.Services.AddSingleton<MainPage>();
            //Inyeccion de los servicios
            builder.Services.AddSingleton<IProductsService, ProductsService>();
            builder.Services.AddSingleton<IOrderServices, OrderService>();
            builder.Services.AddSingleton<IComprasService, ComprasService>();
            builder.Services.AddSingleton<IStockInitService, StockInitService>();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                // Quitar el subrayado (l�nea inferior)
                handler.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);

                // Opcional: quitar tambi�n el fondo por defecto
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);

                DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                {
                    handler.PlatformView.Background = new ColorDrawable(Android.Graphics.Color.Transparent);
                });
#endif
            });


#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            // Guardamos el ServiceProvider
            Services = app.Services;

            return app;

        }
    }
}
