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
            new() { Title = "Crear Nuevo", Icon = "add.png", Action = _ => UiGuard.Run(this, CreateNewAsync) },
            new() { Title = "Inicio", Icon = "warehouse_50px.png", Action = _ => GoToMain() },
            new() { Title = "Parámetros", Icon = "settings_50px.png", Message = "Módulo en construcción" },
            new() { Title = "Sincronizar", Icon = "database_daily_export_50px.png", Action = _ => UiGuard.Run(this, OnSyncAllClickedAsync) },
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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UiGuard.Run(this, LoadDocsAsync);
    }

    private async Task LoadDocsAsync()
    {
        try
        {
            _allDocs = await _service.GetAllAsync();

            // Si no hay documentos, cargar datos fake para mostrar el diseño
            if (_allDocs.Count == 0)
            {
                _allDocs = GetFakeDocs();
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error cargando StockInit: " + ex.Message);
            SummaryLabel.Text = "Error al cargar documentos";
        }
    }

    private List<StockInitEntity> GetFakeDocs()
    {
        return new List<StockInitEntity>
        {
            new StockInitEntity
            {
                Numero = "TMP-0001",
                NumeroServidor = "FIS-0001",
                Fecha = DateTime.Now.AddDays(-5),
                Description = "Inventario de bebidas y alimentos bodega 1",
                Status = "Sincronizado",
                Items = new List<StockItemEntity>
                {
                    new StockItemEntity { Id = 1, Numero = "TMP-0001", Product_Code = "P001", Product_Name = "Coca Cola 500ml", Cantidad = 150, Costo = 1.50, TotalCosto = 225.00, Ubicacion = "A1-01", Nota = "", Enviado = true },
                    new StockItemEntity { Id = 2, Numero = "TMP-0001", Product_Code = "P002", Product_Name = "Gaseosa Sprite 500ml", Cantidad = 120, Costo = 1.50, TotalCosto = 180.00, Ubicacion = "A1-02", Nota = "", Enviado = true },
                    new StockItemEntity { Id = 3, Numero = "TMP-0001", Product_Code = "P003", Product_Name = "Agua Mineral 1L", Cantidad = 200, Costo = 0.80, TotalCosto = 160.00, Ubicacion = "A1-03", Nota = "", Enviado = true }
                }
            },
            new StockInitEntity
            {
                Numero = "TMP-0002",
                NumeroServidor = "FIS-0002",
                Fecha = DateTime.Now.AddDays(-3),
                Description = "Conteo de productos de limpieza y aseo",
                Status = "Actualizado",
                Items = new List<StockItemEntity>
                {
                    new StockItemEntity { Id = 4, Numero = "TMP-0002", Product_Code = "L001", Product_Name = "Detergente 1L", Cantidad = 80, Costo = 3.50, TotalCosto = 280.00, Ubicacion = "B2-01", Nota = "", Enviado = true },
                    new StockItemEntity { Id = 5, Numero = "TMP-0002", Product_Code = "L002", Product_Name = "Desinfectante 500ml", Cantidad = 60, Costo = 2.80, TotalCosto = 168.00, Ubicacion = "B2-02", Nota = "", Enviado = false },
                    new StockItemEntity { Id = 6, Numero = "TMP-0002", Product_Code = "L003", Product_Name = "Jabón Líquido 750ml", Cantidad = 45, Costo = 4.20, TotalCosto = 189.00, Ubicacion = "B2-03", Nota = "Revisar fecha de vencimiento", Enviado = false }
                }
            },
            new StockInitEntity
            {
                Numero = "TMP-0003",
                NumeroServidor = null,
                Fecha = DateTime.Now.AddDays(-1),
                Description = "Conteo de frutas y verduras sección 3",
                Status = "Iniciado",
                Items = new List<StockItemEntity>
                {
                    new StockItemEntity { Id = 7, Numero = "TMP-0003", Product_Code = "F001", Product_Name = "Manzana Roja 1kg", Cantidad = 50, Costo = 2.00, TotalCosto = 100.00, Ubicacion = "C3-01", Nota = "", Enviado = false },
                    new StockItemEntity { Id = 8, Numero = "TMP-0003", Product_Code = "F002", Product_Name = "Plátano 1kg", Cantidad = 35, Costo = 1.20, TotalCosto = 42.00, Ubicacion = "C3-02", Nota = "", Enviado = false }
                }
            },
            new StockInitEntity
            {
                Numero = "TMP-0004",
                NumeroServidor = "FIS-0004",
                Fecha = DateTime.Now.AddDays(-7),
                Description = "Inventario de panadería y repostería",
                Status = "Cerrado",
                Items = new List<StockItemEntity>
                {
                    new StockItemEntity { Id = 9, Numero = "TMP-0004", Product_Code = "PA001", Product_Name = "Pan Francés", Cantidad = 200, Costo = 0.50, TotalCosto = 100.00, Ubicacion = "D4-01", Nota = "", Enviado = true },
                    new StockItemEntity { Id = 10, Numero = "TMP-0004", Product_Code = "PA002", Product_Name = "Croissant", Cantidad = 150, Costo = 0.80, TotalCosto = 120.00, Ubicacion = "D4-02", Nota = "", Enviado = true },
                    new StockItemEntity { Id = 11, Numero = "TMP-0004", Product_Code = "PA003", Product_Name = "Dulce de Leche", Cantidad = 80, Costo = 1.50, TotalCosto = 120.00, Ubicacion = "D4-03", Nota = "", Enviado = true },
                    new StockItemEntity { Id = 12, Numero = "TMP-0004", Product_Code = "PA004", Product_Name = "Muffin de Arándanos", Cantidad = 60, Costo = 1.20, TotalCosto = 72.00, Ubicacion = "D4-04", Nota = "", Enviado = true }
                }
            },
            new StockInitEntity
            {
                Numero = "TMP-0005",
                NumeroServidor = null,
                Fecha = DateTime.Now,
                Description = "Conteo de carnes y embutidos sección 5",
                Status = "Actualizado",
                Items = new List<StockItemEntity>
                {
                    new StockItemEntity { Id = 13, Numero = "TMP-0005", Product_Code = "C001", Product_Name = "Pollo Entero 1kg", Cantidad = 40, Costo = 4.50, TotalCosto = 180.00, Ubicacion = "E5-01", Nota = "", Enviado = false },
                    new StockItemEntity { Id = 14, Numero = "TMP-0005", Product_Code = "C002", Product_Name = "Cerdo 1kg", Cantidad = 25, Costo = 6.00, TotalCosto = 150.00, Ubicacion = "E5-02", Nota = "", Enviado = false },
                    new StockItemEntity { Id = 15, Numero = "TMP-0005", Product_Code = "C003", Product_Name = "Chorizo 500g", Cantidad = 100, Costo = 3.00, TotalCosto = 300.00, Ubicacion = "E5-03", Nota = "", Enviado = false }
                }
            }
        };
    }

    private void ApplyFilter()
    {
        var term = SearchEntry.Text?.Trim() ?? string.Empty;

        var docs = string.IsNullOrEmpty(term)
            ? _allDocs
            : _allDocs.Where(d => d.NumeroVisible.Contains(term, StringComparison.OrdinalIgnoreCase)
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

    private void OnCreatePageDisappearing(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (sender is Page page)
            page.Disappearing -= OnCreatePageDisappearing;
        await LoadDocsAsync();
    });

    private void OnSyncItemClicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if ((sender as BindableObject)?.BindingContext is not StockInitEntity doc) return;

        var confirmed = await DisplayAlertAsync("Sincronizar",
            $"¿Desea sincronizar el documento {doc.Numero} al servidor?", "Sí", "No");
        if (!confirmed) return;

        await SyncDocAsync(doc);
        await LoadDocsAsync();
    });

    private async Task SyncDocAsync(StockInitEntity doc)
    {
        var result = await _service.SincronizarAsync(doc.Numero);
        await DisplayAlertAsync(result.Success ? "Sincronizado" : "Error", result.Message, "OK");
    }

    private void OnSyncAllClicked(object? sender, EventArgs e)
        => UiGuard.Run(this, OnSyncAllClickedAsync);

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

    private void OnDocTapped(object? sender, TappedEventArgs e) => UiGuard.Run(this, async () =>
    {
        if ((sender as BindableObject)?.BindingContext is not StockInitEntity doc) return;

        var detail = MauiProgram.Services!.GetService<StockInitDetailPage>();
        if (detail is null) return;

        detail.SetNumero(doc.Numero);
        if (Detail is NavigationPage nav)
        {
            EventHandler<NavigationEventArgs>? handler = null;
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
    });

    private void OnCloseDocClicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
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
    });

    private void OnViewDocClicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if ((sender as BindableObject)?.BindingContext is not StockInitEntity doc) return;
        await NavigateToDetailAsync(doc.Numero);
    });

    private void OnCreateNewClicked(object? sender, EventArgs e)
        => UiGuard.Run(this, CreateNewAsync);

    private async Task NavigateToDetailAsync(string numero)
    {
        var detail = MauiProgram.Services!.GetService<StockInitDetailPage>();
        if (detail is null) return;

        detail.SetNumero(numero);
        if (Detail is NavigationPage nav)
        {
            EventHandler<NavigationEventArgs>? handler = null;
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
}