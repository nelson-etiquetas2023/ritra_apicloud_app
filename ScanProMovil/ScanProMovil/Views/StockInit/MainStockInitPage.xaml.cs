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
            new() { Title = "Sincronizar", Icon = "database_daily_export_50px.png", Action = async _ => await OnSyncAllClickedAsync() },
        ]);
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        GoToMain();
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

        page.Disappearing += OnCreatePageDisappearing;
        await nav.PushAsync(page);
    }

    private async void OnCreatePageDisappearing(object? sender, EventArgs e)
    {
        if (sender is Page page)
            page.Disappearing -= OnCreatePageDisappearing;
        await LoadDocsAsync();
    }

    private async void OnSyncItemClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not StockInitEntity doc) return;

        var confirmed = await DisplayAlertAsync("Sincronizar",
            $"¿Desea sincronizar el documento {doc.Numero} al servidor?", "Sí", "No");
        if (!confirmed) return;

        await SyncDocAsync(doc);
        await LoadDocsAsync();
    }

    private async Task SyncDocAsync(StockInitEntity doc)
    {
        var result = await _service.SincronizarAsync(doc.Numero);
        await DisplayAlertAsync(result.Success ? "Sincronizado" : "Error", result.Message, "OK");
    }

    private async void OnSyncAllClicked(object? sender, EventArgs e)
        => await OnSyncAllClickedAsync();

    private async Task OnSyncAllClickedAsync()
    {
        var pendientes = _allDocs
            .Where(d => d.Status is not "Sincronizado" && d.Status is not "Cerrado")
            .ToList();

        if (pendientes.Count == 0)
        {
            await DisplayAlertAsync("Sincronizar", "No hay documentos pendientes de sincronizar.", "OK");
            return;
        }

        var confirmed = await DisplayAlertAsync("Sincronizar todo",
            $"Se sincronizarán {pendientes.Count} documento(s) con ítems pendientes. ¿Continuar?", "Sí", "No");
        if (!confirmed) return;

        var okCount = 0;
        var failCount = 0;
        foreach (var doc in pendientes)
        {
            var result = await _service.SincronizarAsync(doc.Numero);
            if (result.Success)
                okCount++;
            else
                failCount++;
        }

        await DisplayAlertAsync("Sincronización",
            okCount > 0 ? $"{okCount} documento(s) sincronizados correctamente." : "",
            "OK");
        if (failCount > 0)
            await DisplayAlertAsync("Atención", $"{failCount} documento(s) no se pudieron sincronizar.", "OK");

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
            EventHandler<NavigationEventArgs> handler = null;
            handler = (s, e) =>
            {
                if (e.Page == detail)
                {
                    nav.Popped -= handler;
                    MainThread.BeginInvokeOnMainThread(async () => await LoadDocsAsync());
                }
            };
            nav.Popped += handler;
            await nav.PushAsync(detail);
        }
    }

    private async void OnCloseDocClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not StockInitEntity doc) return;

        var confirm = await DisplayAlertAsync("Cerrar documento",
            $"¿Cerrar el documento {doc.Numero}? No se podrán agregar más productos ni sincronizar.",
            "Cerrar", "Cancelar");
        if (!confirm) return;

        doc.Status = "Cerrado";

        var ok = await _service.UpdateAsync(doc);

        if (ok)
        {
            await DisplayAlertAsync("Cerrado", $"El documento {doc.Numero} ha sido cerrado.", "OK");
            await LoadDocsAsync();
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo cerrar el documento.", "OK");
        }
    }
}