using ScanProMovil;
using ScanProMovil.Services.StockInicial;
using ScanProMovil.Views;
using StockInitEntity = ScanProMovil.Entities.StockInit;
using StockItemEntity = ScanProMovil.Entities.StockItem;

namespace ScanProMovil.Views.StockInit;

public partial class MainStockInitPage : FlyoutPage
{
    private readonly IStockInitService _service;
    private List<StockInitEntity> _allDocs = new();

    public MainStockInitPage(IStockInitService service)
    {
        InitializeComponent();
        _service = service;

        FlyoutMenuHost.Attach(this,
        [
            new() { Title = "Crear Nuevo", Icon = "add.png", Action = async _ => await CreateNewAsync() },
            new() { Title = "Inicio", Icon = "warehouse_50px.png", Action = _ => GoToMain() },
            new() { Title = "Parámetros", Icon = "settings_50px.png", Message = "Módulo en construcción" },
            new() { Title = "Sincronizar", Icon = "database_daily_export_50px.png", Message = "Módulo en construcción" },
        ]);
    }

    private void GoToMain()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is not null)
            window.Page = new FlyoutMainPage();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDocsAsync();
    }

    private async Task LoadDocsAsync()
    {
        try
        {
            await _service.SeedDummyDataAsync();

            _allDocs = await _service.GetAllAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error cargando StockInit: " + ex.Message);
            SummaryLabel.Text = "Error al cargar documentos";
        }
    }

    private void ApplyFilter()
    {
        var term = SearchEntry.Text?.Trim() ?? string.Empty;

        var docs = string.IsNullOrEmpty(term)
            ? _allDocs
            : _allDocs.Where(d => d.Numero.Contains(term, StringComparison.OrdinalIgnoreCase)
                                || d.Description.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

        DocsCollection.ItemsSource = docs;

        var totalItems = docs.Sum(d => d.Items.Count);
        SummaryLabel.Text = $"{docs.Count} documentos · {totalItems} ítems";
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private async Task CreateNewAsync()
    {
        if (Detail is not NavigationPage nav) return;

        var page = MauiProgram.Services!.GetService<CreateStockInitPage>();
        if (page is null) return;

        await nav.PushAsync(page);
        await LoadDocsAsync();
    }

    private async void OnSyncItemClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not StockInitEntity doc) return;

        var confirmed = await DisplayAlertAsync("Sincronizar",
            $"¿Desea sincronizar el documento {doc.Numero} al servidor?", "Sí", "No");
        if (!confirmed) return;

        var result = await _service.SincronizarAsync(doc.Numero);
        await DisplayAlertAsync(result.Success ? "Sincronizado" : "Error", result.Message, "OK");
        await LoadDocsAsync();
    }

    private async void OnDocTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not StockInitEntity doc) return;

        var detail = MauiProgram.Services!.GetService<StockInitDetailPage>();
        if (detail is null) return;

        detail.SetNumero(doc.Numero);
        if (Detail is NavigationPage nav)
        {
            detail.Disappearing += OnDetailDisappearing;
            await nav.PushAsync(detail);
        }
    }

    private async void OnDetailDisappearing(object? sender, EventArgs e)
    {
        if (sender is Page page)
            page.Disappearing -= OnDetailDisappearing;
        await LoadDocsAsync();
    }
}