using Microsoft.EntityFrameworkCore;
using ScanProMovil.Data;
using ScanProMovil.Services.Session;
using ScanProMovil.Views;
using ScanProMovil.Views.Compras;
using ScanProMovil.Views.Products;
using ScanProMovil.Views.StockInit;

namespace ScanProMovil
{
    public partial class MainPage : ContentPage
    {
        private readonly AppSession _session;

        public MainPage()
        {
            InitializeComponent();
            _session = MauiProgram.Services!.GetRequiredService<AppSession>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadSessionHeader();
            AppDbContext? db = null;
            try
            {
                db = MauiProgram.Services!.GetService<AppDbContext>();
                if (db is not null)
                    await db.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aplicando migraciones EF: {ex.Message}");

                if (db is not null && db.Database.IsSqlite())
                {
                    try
                    {
                        await db.Database.EnsureDeletedAsync();
                        await db.Database.MigrateAsync();
                        System.Diagnostics.Debug.WriteLine("Base de datos recreada con el esquema actual.");
                    }
                    catch (Exception exRec)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reconstruyendo la base: {exRec.Message}");
                    }
                }
            }

            if (db is not null)
                await BackfillTotalesAsync(db);
        }

        private static async Task BackfillTotalesAsync(AppDbContext db)
        {
            try
            {
                var orders = await db.PurchaseOrders
                    .Include(o => o.Items)
                    .Where(o => o.Total == 0 || o.Subtotal == 0)
                    .ToListAsync();

                foreach (var o in orders)
                {
                    o.ItemsNumber = o.Items.Count;
                    o.Subtotal = o.Items.Sum(i => i.Cantidad * i.Costo);
                    o.Impuesto = 0;
                    o.Total = o.Subtotal;
                }

                if (orders.Count > 0)
                {
                    await db.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Backfill de totales: {orders.Count} documentos actualizados.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en backfill de totales: {ex.Message}");
            }
        }

        public async void OnTapProducts(object? sender, TappedEventArgs e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }
            await Navigation.PushAsync(new ProductsPage());
        }

        private async void OnTapSetting(object? sender, TappedEventArgs e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }
            await Navigation.PushAsync(new SettingPage());
        }

        private async void OnTapOrdersModule(object? sender, TappedEventArgs? e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }

            var  page = MauiProgram.Services!.GetService<OrdersPage>();
            await Navigation.PushAsync(page!);
        }

        private async  void OnTapApiModule(object? sender, TappedEventArgs? e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }
            await Navigation.PushAsync(new ApiPage());
        }
        private async void SqliteModule(object? sender, TappedEventArgs? e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }

            await Navigation.PushAsync(new SqliteCrudPage());
        }

        private async void OnTapScanQrCode(object? sender, TappedEventArgs? e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }
            await Navigation.PushAsync(new CodeQrScan());
        }

        private async void OnTapSincroData(object? sender, TappedEventArgs? e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }
            await Navigation.PushAsync(new SincroDocuments());

        }

        private async void OnLoginUsers(object? sender, EventArgs? e)
        {
            await DisplayAlertAsync("mensaje", "modulo de login...", "OK");
        }

        private async void OnTapShopping(object? sender, TappedEventArgs? e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }

            var page = MauiProgram.Services!.GetService<ShoppingPage>();
            await Navigation.PushAsync(page!);
            
        }

        private async void TapGestureRecognizer_Compras(object? sender, TappedEventArgs? e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }

            var page = MauiProgram.Services!.GetService<ComprasIndexPage>();
            await Navigation.PushAsync(page!);
        }

        private void LoadSessionHeader()
        {
            if (_session is null) return;

            var userName = string.IsNullOrWhiteSpace(_session.UserName)
                ? (_session.UserEmail ?? "Anónimo")
                : _session.UserName;

            LabelHeaderUser.Text = userName;
            LabelHeaderDevice.Text = _session.DeviceDisplayName;
            LabelHeaderWarehouse.Text = $"Almacén: {_session.WarehouseName}";
        }

        private void OnToggleFlyout(object? sender, TappedEventArgs e)
        {
            Element? element = this;
            while (element is not null && element is not FlyoutPage)
                element = element.Parent;

            if (element is FlyoutPage flyoutPage)
                flyoutPage.IsPresented = !flyoutPage.IsPresented;
        }

        private async void OnTapStockInit(object? sender, TappedEventArgs? e)
        {
            if (sender is Image img)
            {
                await img.FadeToAsync(0.5, 100); // baja opacidad
                await img.FadeToAsync(1, 100);   // vuelve a normal
            }

            var page = MauiProgram.Services!.GetService<MainStockInitPage>();
            if (page is null) return;

            // El FlyoutPage debe ser raiz de la ventana para que el menú hamburguesa funcione.
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window is not null)
                window.Page = page;
            else
                await Navigation.PushAsync(page);
        }
    }
}
