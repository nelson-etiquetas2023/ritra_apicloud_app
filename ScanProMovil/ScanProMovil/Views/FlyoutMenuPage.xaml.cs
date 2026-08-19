using ScanProMovil.Services.Session;
using ScanProMovil.Views.Compras;
using ScanProMovil.Views.Products;
using ScanProMovil.Views.StockInit;

namespace ScanProMovil.Views;

public partial class FlyoutMenuPage : ContentPage
{
    private readonly AppSession _session;

    public event EventHandler<FlyoutMenuItem>? MenuItemSelected;
    public event EventHandler? LogoutRequested;

    public static List<FlyoutMenuItem> DefaultItems { get; } =
    [
        new() { Title = "Compras", Icon = "buy_50px.png", TargetType = typeof(ComprasIndexPage) },
        new() { Title = "Products", Icon = "package_delivery_logistics_50px.png", TargetType = typeof(ProductsPage) },
        new() { Title = "Orders", Icon = "purchase_order_50px.png", TargetType = typeof(OrdersPage) },
        new() { Title = "Shopping", Icon = "buying.png", TargetType = typeof(ShoppingPage) },
        new() { Title = "Stock Inicial", Icon = "move_stock_50px.png", TargetType = typeof(MainStockInitPage) },
        new() { Title = "Sincro Data", Icon = "database_daily_export_50px.png", TargetType = typeof(SincroDocuments) },
        new() { Title = "Test Api", Icon = "rest_api_50px.png", TargetType = typeof(ApiPage) },
        new() { Title = "Código QR", Icon = "qr_code_50px.png", TargetType = typeof(CodeQrScan) },
        new() { Title = "Configuración", Icon = "settings_50px.png", TargetType = typeof(SettingPage) },
    ];

    public FlyoutMenuPage(IReadOnlyList<FlyoutMenuItem>? topItems = null, bool includeDefaults = true)
    {
        InitializeComponent();
        _session = MauiProgram.Services!.GetRequiredService<AppSession>();

        var items = new List<FlyoutMenuItem>();
        if (topItems is not null)
            items.AddRange(topItems);
        if (includeDefaults)
            items.AddRange(DefaultItems);

        MenuCollection.ItemsSource = items;
        LoadSession();
    }

    private void LoadSession()
    {
        UserNameLabel.Text = _session.UserName ?? "Usuario";
        DeviceLabel.Text = _session.DeviceDisplayName;
        WarehouseLabel.Text = $"Almacén: {_session.WarehouseName}";
    }

    private void OnMenuItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FlyoutMenuItem item)
            MenuItemSelected?.Invoke(this, item);

        MenuCollection.SelectedItem = null;
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
        => LogoutRequested?.Invoke(this, EventArgs.Empty);
}