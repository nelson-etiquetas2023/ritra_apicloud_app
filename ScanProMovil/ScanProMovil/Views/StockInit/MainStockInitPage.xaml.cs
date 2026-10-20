using ScanProMovil.Services.StockInicial;
using ScanProMovil.ViewModels;

namespace ScanProMovil.Views.StockInit;

public partial class MainStockInitPage : FlyoutPage
{
    private readonly StockInitViewModel _vm;
    private readonly IStockInitService _service;

    public MainStockInitPage(StockInitViewModel vm, IStockInitService service)
    {
        InitializeComponent();
        _vm = vm;
        _service = service;

        // El ContentPage interior vive dentro de x:Arguments: se le asigna el
        // BindingContext del ViewModel para los bindings compilados.
        if (Detail is NavigationPage nav && nav.Navigation.NavigationStack.FirstOrDefault() is ContentPage detail)
            detail.BindingContext = _vm;

        FlyoutMenuHost.Attach(this,
        [
            new() { Title = "Crear Nuevo", Icon = "add.png", Action = _ => UiGuard.Run(this, CreateNewAsync) },
            new() { Title = "Inicio", Icon = "warehouse_50px.png", Action = _ => GoToMain() },
            new() { Title = "Parámetros", Icon = "settings_50px.png", Message = "Módulo en construcción" },
            new() { Title = "Sincronizar", Icon = "database_daily_export_50px.png", Action = _ => UiGuard.Run(this, OnSyncAllAsync) },
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
        UiGuard.Run(this, async () =>
        {
            if (_vm.RefreshListDocs)
            {
                await Task.Yield();
                await _vm.GetDocsLocalSqliteCommand.ExecuteAsync(null);
            }
        });
    }

    // ---------------- Toolbar de acciones (operan sobre SelectedDoc) ----------------

    private void btnAddDoc_Clicked(object? sender, EventArgs e) => UiGuard.Run(this, CreateNewAsync);

    private void btnInfoDoc_Clicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (_vm.SelectedDoc is null)
        {
            await DisplayAlertAsync("Warning", "Tiene que seleccionar un documento de la Lista...", "Ok.");
            return;
        }

        await NavigateToDetailAsync(_vm.SelectedDoc.Numero);
    });

    private void btnConfig_Clicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        await DisplayAlertAsync("Parámetros", "Módulo en construcción", "OK");
    });

    private void btnSincDoc_Clicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (_vm.SelectedDoc is null)
        {
            await DisplayAlertAsync("Warning", "Tiene que seleccionar un documento de la Lista...", "Ok.");
            return;
        }

        var doc = _vm.SelectedDoc;

        if (string.Equals(doc.Status, "Cerrado", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync("Documento cerrado",
                "El documento está cerrado, no se puede sincronizar.", "Ok.");
            return;
        }

        if (doc.Sincronizado && doc.PendientesCount == 0)
        {
            await DisplayAlertAsync("Documento sincronizado",
                "El documento ya fue sincronizado...", "Ok.");
            return;
        }

        var confirmed = await DisplayAlertAsync("Sincronizar",
            $"¿Desea sincronizar el documento {doc.NumeroVisible} al servidor?", "Sí", "No");
        if (!confirmed) return;

        var result = await _service.SincronizarAsync(doc.Numero);
        await DisplayAlertAsync(result.Success ? "Sincronizado" : "Error", result.Message, "OK");

        await ReloadAsync();
    });

    private void btnDeleteDoc_Clicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (_vm.SelectedDoc is null)
        {
            await DisplayAlertAsync("Warning", "Tiene que seleccionar un documento de la Lista...", "Ok.");
            return;
        }

        var doc = _vm.SelectedDoc;

        if (doc.Sincronizado)
        {
            await DisplayAlertAsync("Documento sincronizado",
                "El documento ya fue sincronizado, no se puede eliminar.", "Ok.");
            return;
        }

        var confirm = await DisplayAlertAsync("Eliminar documento",
            $"¿Seguro que desea eliminar el documento {doc.NumeroVisible}?", "Eliminar", "Cancelar");
        if (!confirm) return;

        _vm.SelectedDoc = null;
        var deleted = await _service.DeleteAsync(doc.Numero);
        if (deleted)
        {
            await ReloadAsync();
            await DisplayAlertAsync("Documento eliminado",
                $"El documento {doc.NumeroVisible} fue eliminado.", "Ok.");
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo eliminar el documento.", "Ok.");
        }
    });

    private void btnCancelDoc_Clicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (_vm.SelectedDoc is null)
        {
            await DisplayAlertAsync("Warning", "Tiene que seleccionar un documento de la Lista...", "Ok.");
            return;
        }

        var doc = _vm.SelectedDoc;

        if (string.Equals(doc.Status, "Cerrado", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync("Documento cerrado", "El documento ya está cerrado.", "Ok.");
            return;
        }

        var confirm = await DisplayAlertAsync("Cerrar documento",
            $"¿Cerrar el documento {doc.NumeroVisible}? No se podrán agregar más productos ni sincronizar.",
            "Cerrar", "Cancelar");
        if (!confirm) return;

        doc.Status = "Cerrado";
        var ok = await _service.UpdateAsync(doc);
        if (ok)
        {
            await ReloadAsync();
            await DisplayAlertAsync("Cerrado", $"El documento {doc.NumeroVisible} ha sido cerrado.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo cerrar el documento.", "OK");
        }
    });

    private async Task CreateNewAsync()
    {
        if (Detail is not NavigationPage nav) return;

        _vm.RefreshListDocs = true;
        var page = MauiProgram.Services!.GetService<CreateStockInitPage>();
        if (page is null) return;

        page.Disappearing += OnCreatePageDisappearing;
        await nav.PushAsync(page);
    }

    private void OnCreatePageDisappearing(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (sender is Page page)
            page.Disappearing -= OnCreatePageDisappearing;
        await ReloadAsync();
    });

    private async Task NavigateToDetailAsync(string numero)
    {
        if (Detail is not NavigationPage nav) return;

        var detail = MauiProgram.Services!.GetService<StockInitDetailPage>();
        if (detail is null) return;

        detail.SetNumero(numero);

        EventHandler<NavigationEventArgs>? handler = null;
        handler = (s, e) =>
        {
            if (e.Page == detail)
            {
                nav.Popped -= handler;
                MainThread.BeginInvokeOnMainThread(async () => await ReloadAsync());
            }
        };
        nav.Popped += handler;
        await nav.PushAsync(detail);
    }

    private async Task OnSyncAllAsync()
    {
        var allDocs = await _service.GetAllAsync();
        var pendientes = allDocs
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
            okCount > 0 ? $"{okCount} documento(s) sincronizados correctamente." : "", "OK");
        if (failCount > 0)
            await DisplayAlertAsync("Atención", $"{failCount} documento(s) no se pudieron sincronizar.", "OK");

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _vm.RefreshListDocs = true;
        await _vm.GetDocsLocalSqliteCommand.ExecuteAsync(null);
    }
}


