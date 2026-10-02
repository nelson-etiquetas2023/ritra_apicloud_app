using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Products;
using ScanProMovil.Services.StockInicial;
using StockInitEntity = ScanProMovil.Entities.StockInit;
using StockItemEntity = ScanProMovil.Entities.StockItem;

namespace ScanProMovil.Views.StockInit;

public partial class StockInitDetailPage : ContentPage
{
    private readonly IStockInitService _service;
    private readonly IProductsService _productsService;
    private string? _numero;
    private StockInitEntity? _doc;
    private Product? _productScan;
    private List<StockItemEntity> _allItems = new();
    private CancellationTokenSource? _scanDebounceCts;
    private bool _promptOpen;

    public StockInitDetailPage(IStockInitService service, IProductsService productsService)
    {
        InitializeComponent();
        _service = service;
        _productsService = productsService;

        ScanEntry.Loaded += OnScanEntryLoaded;
    }

    public void SetNumero(string numero) => _numero = numero;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UiGuard.Run(this, async () =>
        {
            if (_doc is not null) return;
            if (string.IsNullOrWhiteSpace(_numero)) return;

            var doc = await _service.GetByIdAsync(_numero);
            if (doc is null) return;

            _doc = doc;
            NumeroLabel.Text = doc.NumeroVisible;
            RefreshItems();
        });
    }

    private void OnScanEntryLoaded(object? sender, EventArgs e)
    {
#if ANDROID
        AttachEscapeHandler(ScanEntry);
#endif
    }

#if ANDROID
    private void AttachEscapeHandler(Entry entry)
    {
        if (entry.Handler?.PlatformView is not Android.Widget.EditText editText) return;
        editText.KeyPress -= OnEditKeyPress;
        editText.KeyPress += OnEditKeyPress;
    }

    private void OnEditKeyPress(object? sender, Android.Views.View.KeyEventArgs e)
    {
        if (e.KeyCode is Android.Views.Keycode.Escape or Android.Views.Keycode.Back)
        {
            if (ScanRow.IsVisible)
            {
                HideScanRow();
                e.Handled = true;
            }
        }
    }
#endif

    private void OnAddLineClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[SCAN] OnAddLineClicked called");
        if (_doc is null) return;
        
        if (string.Equals(_doc.Status, "Cerrado", StringComparison.OrdinalIgnoreCase))
        {
            DisplayAlertAsync("Documento cerrado", "Este documento está cerrado. No se pueden agregar más productos.", "OK");
            return;
        }
        
        ShowScanRow();
        ShowSaveButton();
    }

    private void FocusAndShowKeyboard(Entry entry)
    {
        Dispatcher.Dispatch(() =>
        {
            entry.Focus();
#if ANDROID
            if (entry.Handler?.PlatformView is Android.Widget.EditText et)
            {
                et.RequestFocus();
                var imm = (Android.Views.InputMethods.InputMethodManager?)
                    Android.App.Application.Context.GetSystemService(Android.Content.Context.InputMethodService);
                imm?.ShowSoftInput(et, Android.Views.InputMethods.ShowFlags.Forced);
            }
#endif
        });
    }

    private void ShowScanRow()
    {
        System.Diagnostics.Debug.WriteLine("[SCAN] ShowScanRow called");
        _productScan = null;
        SearchRow.IsVisible = false;
        ScanRow.IsVisible = true;
        ScanEntry.Text = string.Empty;
        ScanProductLabel.IsVisible = false;
        FocusAndShowKeyboard(ScanEntry);
    }

    private void HideScanRow()
    {
        _productScan = null;
        ScanRow.IsVisible = false;
        SearchRow.IsVisible = true;
        ScanEntry.Text = string.Empty;
        ScanProductLabel.IsVisible = false;
        ApplyItemFilter();
        ItemSearchEntry.Focus();
    }

    private void OnItemSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue;
        if (string.IsNullOrWhiteSpace(text)) return;

        if (text.StartsWith('*') && text.EndsWith('*'))
        {
            ItemSearchEntry.Text = text.Trim('*');
            return;
        }

        ApplyItemFilter();
    }

    private void ApplyItemFilter()
    {
        if (_doc is null) return;

        var term = ItemSearchEntry.Text?.Trim() ?? string.Empty;
        List<StockItemEntity> filtered;
        if (string.IsNullOrEmpty(term))
        {
            filtered = _allItems;
        }
        else
        {
            var lower = term.ToLower();
            filtered = _allItems.Where(i =>
                i.Product_Code.ToLower().Contains(lower) ||
                i.Product_Name.ToLower().Contains(lower) ||
                (i.Nota != null && i.Nota.ToLower().Contains(lower))).ToList();
        }

        ItemsCollection.ItemsSource = filtered;
        var filteredUnits = filtered.Sum(i => i.Cantidad);
        var totalUnits = _allItems.Sum(i => i.Cantidad);
        ItemsCountLabel.Text =
            $"{filtered.Count} de {_allItems.Count} ítems · {filteredUnits:N2} de {totalUnits:N2} und";
    }

    private void OnScanTextChanged(object? sender, TextChangedEventArgs e) => UiGuard.Run(this, async () =>
    {
        var text = e.NewTextValue;
        System.Diagnostics.Debug.WriteLine($"[SCAN] OnScanTextChanged: '{text}'");
        if (string.IsNullOrWhiteSpace(text))
        {
            _scanDebounceCts?.Cancel();
            ScanProductLabel.IsVisible = false;
            return;
        }

        var trimmed = text.Trim();
        if (trimmed.Length <= 2) return;

        if (trimmed.StartsWith('*') && trimmed.EndsWith('*'))
        {
            _scanDebounceCts?.Cancel();
            ScanEntry.Text = trimmed.Trim('*');
            await ResolveProductAsync(trimmed.Trim('*'));
            return;
        }

        _scanDebounceCts?.Cancel();
        var cts = _scanDebounceCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(500, cts.Token);
            await ResolveProductAsync(trimmed);
        }
        catch (OperationCanceledException)
        {
        }
    });

    private void OnScanCompleted(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        var code = ScanEntry.Text?.Trim('*', ' ', '\r', '\n') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code)) return;

        _scanDebounceCts?.Cancel();
        await ResolveProductAsync(code);
    });

    private async Task ResolveProductAsync(string codebar)
    {
        System.Diagnostics.Debug.WriteLine($"[SCAN] ResolveProductAsync called with: '{codebar}'");
        
        _productScan = await _productsService.GetProductLocalById(codebar)
            ?? await _productsService.GetProductLocalByCode(codebar);

        if (_productScan is not null)
        {
            System.Diagnostics.Debug.WriteLine($"[SCAN] Producto encontrado: {_productScan.Product_Name} (CodeBar={_productScan.CodeBar}, product_code={_productScan.product_code})");
            ScanProductLabel.Text = _productScan.Product_Name;
            ScanProductLabel.TextColor = Color.FromArgb("#0D6EFD");
            ScanProductLabel.IsVisible = true;
            await PromptQuantityAsync();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[SCAN] Producto NO encontrado para: '{codebar}'");
            _productScan = null;
            ScanProductLabel.Text = $"Producto {codebar} no encontrado";
            ScanProductLabel.TextColor = Color.FromArgb("#B02A37");
            ScanProductLabel.IsVisible = true;
        }
    }

    private async Task PromptQuantityAsync()
    {
        if (_promptOpen) return;
        _promptOpen = true;
        try
        {
            if (_productScan is null || _doc is null) return;

                var popup = new QuantityPopup(_productScan.Product_Name);
                await this.ShowPopupAsync(popup, new PopupOptions
                {
                    CanBeDismissedByTappingOutsideOfPopup = false
                }, CancellationToken.None);

                if (popup.Cantidad <= 0)
                {
                    ResetScan();
                    return;
                }

                await AddScannedItemAsync(popup.Cantidad);
                return;
        }
        finally
        {
            _promptOpen = false;
        }
    }

    private void OnAddItemClicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (_doc is null) return;

        if (_productScan is null)
        {
            var code = ScanEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                await DisplayAlertAsync("Producto", "Escaneé o ingrese un código de producto.", "OK");
                return;
            }
            await ResolveProductAsync(code);
            if (_productScan is null) return;
        }

        await PromptQuantityAsync();
    });

    private async Task AddScannedItemAsync(double cantidad)
    {
        if (_doc is null) return;

        if (string.Equals(_doc.Status, "Cerrado", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync("Documento cerrado",
                $"El documento {_doc.Numero} está cerrado. No se pueden agregar productos.", "OK");
            return;
        }

        if (_productScan is null) return;

        // Un producto contado en varias pasadas se acumula en una sola línea:
        // el servidor fusiona por código, así que dos líneas locales
        // dejarían fuera todas menos la última.
        var existente = _doc.Items.FirstOrDefault(i =>
            string.Equals(i.Product_Code?.Trim(), _productScan.product_code?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (existente is not null)
        {
            existente.Cantidad = Math.Round(existente.Cantidad + cantidad, 3);
            existente.TotalCosto = Math.Round(existente.Cantidad * existente.Costo, 2);
            existente.Enviado = false;
            _doc.Status = "Actualizado";

            if (await _service.UpdateAsync(_doc))
            {
                RefreshItems();
                ResetScan();
                ShowSaveButton();
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo actualizar el producto.", "OK");
            }

            return;
        }

        var item = new StockItemEntity
        {
            Numero = _doc.Numero,
            Product_Code = _productScan.product_code,
            Product_Name = _productScan.Product_Name,
            Cantidad = cantidad,
            Costo = _productScan.Costo,
            TotalCosto = Math.Round(cantidad * _productScan.Costo, 2),
            Ubicacion = "SIN UBICACIÓN",
            Nota = string.Empty
        };

        _doc.Items.Add(item);
        _doc.Status = "Actualizado";

        if (await _service.UpdateAsync(_doc))
        {
            RefreshItems();
            ResetScan();
            ShowSaveButton();
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo guardar el producto.", "OK");
        }
    }

    private void OnNoteItemClicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (sender is not ImageButton btn || btn.CommandParameter is not StockItemEntity item) return;
        if (_doc is null) return;

        var nota = await DisplayPromptAsync("Nota del producto",
            $"Producto: {item.Product_Name}\nIngrese una nota:",
            "Guardar", "Cancelar", item.Nota ?? string.Empty, maxLength: 200);
        if (nota is null) return;

        item.Nota = nota.Trim();
        item.Enviado = false;

        if (await _service.UpdateAsync(_doc))
        {
            RefreshItems();
            ShowSaveButton();
        }
        else
            await DisplayAlertAsync("Error", "No se pudo guardar la nota.", "OK");
    });

    private void OnDeleteItemClicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (sender is not ImageButton btn || btn.CommandParameter is not StockItemEntity item) return;
        if (_doc is null) return;

        var confirm = await DisplayAlertAsync("Eliminar renglón",
            $"¿Eliminar el producto {item.Product_Name}?", "Eliminar", "Cancelar");
        if (!confirm) return;

        _doc.Items.Remove(item);

        if (await _service.UpdateAsync(_doc))
        {
            RefreshItems();
            ShowSaveButton();
        }
        else
            await DisplayAlertAsync("Error", "No se pudo eliminar el renglón.", "OK");
    });

    private void RefreshItems()
    {
        if (_doc is null) return;
        _allItems = _doc.Items.ToList();
        ApplyItemFilter();
    }

    private void ResetScan()
    {
        _scanDebounceCts?.Cancel();
        _productScan = null;
        ScanEntry.Text = string.Empty;
        ScanProductLabel.IsVisible = false;
        FocusAndShowKeyboard(ScanEntry);
    }

    private void ShowSaveButton()
    {
        BtnSave.IsVisible = true;
        BtnAddLine.IsVisible = false;
    }

    private void OnSaveClicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (_doc is null) return;

        SavingOverlay.IsVisible = true;
        SavingIndicator.IsRunning = true;
        SavingIndicator.IsVisible = true;
        BtnSave.IsEnabled = false;

        try
        {
            var saveTask = _service.UpdateAsync(_doc);
            await Task.WhenAll(saveTask, Task.Delay(400));
            var ok = await saveTask;

            if (ok)
                await Navigation.PopAsync();
            else
                await DisplayAlertAsync("Error", "No se pudo guardar el documento.", "OK");
        }
        finally
        {
            // El overlay se oculta siempre: si falla, la pantalla no queda congelada.
            SavingIndicator.IsRunning = false;
            SavingIndicator.IsVisible = false;
            SavingOverlay.IsVisible = false;
            BtnSave.IsEnabled = true;
        }
    });
}