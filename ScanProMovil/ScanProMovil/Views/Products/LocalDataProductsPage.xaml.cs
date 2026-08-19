using ScanProMovil.ViewModels.Products;
using System.Diagnostics;

namespace ScanProMovil.Views.Products;

public partial class LocalDataProductsPage : ContentPage
{
    private readonly ProductsLocalViewModels _vm;
 
    public LocalDataProductsPage(ProductsLocalViewModels vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = _vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.GetProductLocal();
        SearchEntry.Focus();
    }

    private async void ReloadProducts(object? sender, EventArgs? e)
    {
        await _vm.GetProductLocal();
        SearchEntry.Text = "";
    }

    private async void searchEntry_TextChanged(object? sender, TextChangedEventArgs? e)
    {
        //busqueda solo codigo de barra
        var text = e!.NewTextValue;
        if (string.IsNullOrWhiteSpace(text)) return;
        //varifico si es un codigo de barra.
        if (text.StartsWith('*') && text.EndsWith('*')) 
        {
            Debug.WriteLine("valor de Codigo de Barra:" + text);
            var textClear = text.Trim("*").ToString();
            _vm.SearchText = textClear;
            //await DisplayAlertAsync("advertencia", "Codigo de Barra: " + textClear, "Ok.");
            await _vm.SearchProductsCommand.ExecuteAsync(null);
            SearchEntry.Text = "";
            _vm.SearchText = "";
        }
    }

    private async void searchEntry_Completed(object? sender, EventArgs? e)
    {
        //busqueda solo por teclado.
        if (SearchEntry.Text.StartsWith('*') && SearchEntry.Text.EndsWith('*')) return;

        //await DisplayAlertAsync("advertencia", "valor por teclado: " + SearchEntry.Text, "Ok.");
        await _vm.SearchProductsCommand.ExecuteAsync(null);
        SearchEntry.Text = "";
        _vm.SearchText = "";
        SearchEntry.Focus();
    }
}