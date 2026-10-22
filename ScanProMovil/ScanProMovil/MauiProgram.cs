using Android.Graphics.Drawables;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using System.Net.Http;
using ScanProMovil.Data;
using ScanProMovil.Services;
using ScanProMovil.Services.Auth;
using ScanProMovil.Services.Compras;
using ScanProMovil.Services.Orders;
using ScanProMovil.Services.Picking;
using ScanProMovil.Services.Products;
using ScanProMovil.Services.Logging;
using ScanProMovil.Services.Session;
using ScanProMovil.Services.StockInicial;
using ScanProMovil.ViewModels;
using ScanProMovil.ViewModels.Compras;
using ScanProMovil.ViewModels.Products;
using ScanProMovil.Views;
using ScanProMovil.Views.Compras;
using ScanProMovil.Views.Orders;
using ScanProMovil.Views.Picking;
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

            // URL de la API configurable: se lee de Preferences; solo se siembra el
            // valor por defecto si nunca se ha guardado (no se sobreescribe en cada arranque).
            const string defaultApiUrl = "http://192.168.10.10:8080";
            const string apiBaseUrlKey = "ApiBaseUrl";
            if (!Preferences.ContainsKey(apiBaseUrlKey) ||
                string.IsNullOrWhiteSpace(Preferences.Get(apiBaseUrlKey, string.Empty)))
            {
                Preferences.Set(apiBaseUrlKey, defaultApiUrl);
            }
            var deployLocalApi = Preferences.Get(apiBaseUrlKey, defaultApiUrl).TrimEnd('/');
            var builder = MauiApp.CreateBuilder();


            builder.Services.AddSingleton<AuthSession>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<AppSession>();
            builder.Services.AddTransient<AuthHeaderHandler>();

            builder.Services.AddHttpClient("scanpro", options => {
                options.BaseAddress = new Uri(deployLocalApi);
                options.Timeout = TimeSpan.FromSeconds(8);
                options.DefaultRequestHeaders.Add("User-Agent", "MauiApp");
                options.DefaultRequestHeaders.ConnectionClose = false;
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                EnableMultipleHttp2Connections = true
            }).AddHttpMessageHandler<AuthHeaderHandler>();

            //Entity Framework Core - SQLite (CodeFirst + Migraciones).
            // Se registra el factory para que cada operación cree su propio contexto:
            // EF Core NO es thread-safe y el DbContext singleton compartido entre
            // servicios/procesos paralelos podía lanzar "A second operation was started
            // on this context instance...". AddDbContext se mantiene por compatibilidad
            // con resoluciones puntuales (una sola instancia, no usar en servicios).
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "scanpro.db");
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath};Foreign Keys=True;Cache=Shared")
                       .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking),
                ServiceLifetime.Scoped);

            //Injeccion de los VIEWMODELS.
            builder.Services.AddTransient<ProductsViewModel>();
            builder.Services.AddTransient<OrderViewModel>();
            builder.Services.AddTransient<OrderConfigViewModel>();
            builder.Services.AddTransient<SincroOrdersViewModel>();
            builder.Services.AddTransient<ComprasViewModel>();
            builder.Services.AddTransient<SincroComprasViewModels>();
            builder.Services.AddTransient<ProductsLocalViewModels>();
            builder.Services.AddTransient<StockInitViewModel>();
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
            builder.Services.AddSingleton<IAlertaNotifier, AlertaNotifier>();
            builder.Services.AddTransient<IPickingService, PickingService>();
            builder.Services.AddTransient<PickingViewModel>();
            builder.Services.AddTransient<PickingPage>();
            builder.Services.AddTransient<PickingDetailViewModel>();
            builder.Services.AddTransient<PickingDetailPage>();

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

            // Red global de excepciones (según la wiki oficial de MAUI en Android todo
            // escapa por AppDomain.UnhandledException). No evita el cierre de la app,
            // pero deja el detalle en logs/app-YYYYMMDD.log para diagnóstico en campo.
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var ex = args.ExceptionObject as Exception ?? new Exception(args.ExceptionObject?.ToString() ?? "Excepción desconocida");
                AppLog.Error("AppDomain.UnhandledException", ex);
            };
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                AppLog.Error("TaskScheduler.UnobservedTaskException",
                    args.Exception ?? new Exception("Task faulted sin detalle"));
                args.SetObserved(); // evita que termine el proceso por un Task olvidado
            };

            return app;

        }
    }
}

