using ScanProMovil.ViewModels.Products;
using System.Diagnostics;

namespace ScanProMovil.Views.Products;

public partial class LocalDataProductsPage : ContentPage
{
    private readonly ProductsLocalViewModels _vm;
    private CancellationTokenSource? _scanDebounceCts;
 
    public LocalDataProductsPage(ProductsLocalViewModels vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = _vm;
		_scanDebounceCts = new CancellationTokenSource();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UiGuard.Run(this, async () =>
        {
            await _vm.GetProductLocal();
            SearchEntry.Focus();
        });
    }

    private void OnProductSelected(object? sender, SelectionChangedEventArgs e) => UiGuard.Run(this, async () =>
    {
        if (_vm.SelectedProduct is null) return;

        var product = _vm.SelectedProduct;
        _vm.SelectedProduct = null;
        // Limpia selección visual para permitir re-selección del mismo item
        if (sender is CollectionView cv) cv.SelectedItem = null;
        await Navigation.PushAsync(new ProductDetails(product));
    });

    private void OnProductTapped(object? sender, TappedEventArgs e) => UiGuard.Run(this, async () =>
    {
        if (e.Parameter is not ScanProMovil.Data.Entities.Product product) return;
        // Evita doble navegación si SelectionChanged ya disparó
        if (sender is Grid) await Task.Delay(50);
        _vm.SelectedProduct = null;
        if (cv_localProducts is not null) cv_localProducts.SelectedItem = null;
        await Navigation.PushAsync(new ProductDetails(product));
    });

    private void ReloadProducts(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        await _vm.GetProductLocal();
        SearchEntry.Text = "";
    });

    private void searchEntry_TextChanged(object? sender, TextChangedEventArgs? e) => UiGuard.Run(this, async () =>
    {
        var text = e!.NewTextValue ?? "";
        Debug.WriteLine($"[LocalSearch] TextChanged: '{text}'");
        if (string.IsNullOrWhiteSpace(text))
        {
            _scanDebounceCts?.Cancel();
            return;
        }

        var trimmed = text.Trim();
        // Scan Zebra con * → Enter automático sin pulsar Enter
        if (trimmed.Contains('*'))
        {
            var clean = trimmed.Trim('*', ' ', '\r', '\n', '\t').Trim();
            Debug.WriteLine($"[LocalSearch] Scan * detectado raw='{text}' clean='{clean}'");
            if (clean.Length >= 1)
            {
                _scanDebounceCts?.Cancel();
                SearchEntry.Text = string.Empty;
                await _vm.SearchProductsDirectAsync(clean);
                _vm.SearchText = string.Empty;
                SearchEntry.Text = string.Empty;
                SearchEntry.Focus();
                Debug.WriteLine($"[LocalSearch] Enter auto OK - buscó '{clean}'");
                return;
            }
            return;
        }

        // Teclado sin * NO auto-busca — espera Completed (Enter) "pasta+Enter"
    });

    private void searchEntry_Completed(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        var txt = SearchEntry.Text?.Trim() ?? "";
        Debug.WriteLine($"[LocalSearch] Completed: '{txt}' SearchText='{_vm.SearchText}'");
        if (string.IsNullOrWhiteSpace(txt))
        {
            // Viene de un scan ya procesado en TextChanged (entry ya limpiado) — no re-buscar
            SearchEntry.Focus();
            return;
        }
        if (txt.StartsWith('*') && txt.EndsWith('*') && txt.Length > 2)
        {
            // Scan que no fue capturado en TextChanged (ej. pegado) — procesa aquí
            var clean = txt.Trim('*', ' ', '\r', '\n').Trim();
            _vm.SearchText = clean;
            await _vm.SearchProductsCommand.ExecuteAsync(null);
            SearchEntry.Text = "";
            _vm.SearchText = "";
            SearchEntry.Focus();
            return;
        }

        // Búsqueda por teclado (sin *) — solo con Enter: "pasta+Enter"
        _vm.SearchText = txt;
        await _vm.SearchProductsCommand.ExecuteAsync(null);
        SearchEntry.Text = "";
        _vm.SearchText = "";
        SearchEntry.Focus();
    });
}