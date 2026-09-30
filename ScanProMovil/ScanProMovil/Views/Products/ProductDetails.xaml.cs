using ScanProMovil.Data.Entities;
using ScanProMovil.ViewModels;

namespace ScanProMovil.Views.Products;

public partial class ProductDetails : ContentPage
{
	public ProductDetails(Product product)
	{
		InitializeComponent();

		BindingContext = ActivatorUtilities.CreateInstance<ProductDetailsViewModel>
			(MauiProgram.Services!, product);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Foco directo al campo CodeBar para escaneo inmediato con MC93
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(300);
            txt_codebar?.Focus();
        });
    }

    private bool _codeBarClearedOnFocus = false;

    private void OnCodeBarFocused(object? sender, FocusEventArgs e)
    {
        // Al enfocar para cambiar, deja el valor vacío "" para escaneo directo
        if (!_codeBarClearedOnFocus && BindingContext is ViewModels.ProductDetailsViewModel vm)
        {
            _codeBarClearedOnFocus = true;
            vm.Producto.CodeBar = "";
            if (sender is Entry entry) entry.Text = "";
        }
    }

    private void OnCodeBarTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue?.Trim() ?? "";
        // MC93 envía *CODIGO* - detecta y limpia asteriscos
        if (text.Length >= 3 && text.StartsWith('*') && text.EndsWith('*'))
        {
            var clean = text.Trim('*').Trim();
            if (!string.IsNullOrWhiteSpace(clean) && BindingContext is ViewModels.ProductDetailsViewModel vm)
            {
                vm.Producto.CodeBar = clean;
                txt_codebar.Text = clean;
                txt_codebar.CursorPosition = clean.Length;
            }
        }
    }

    private void OnCodeBarCompleted(object? sender, EventArgs e)
    {
        // Enter/Done -> opcional: disparar guardado si quieres, por ahora solo quita foco
        txt_codebar?.Unfocus();
    }
}