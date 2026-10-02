using ScanProMovil.Data.Entities;
using ScanProMovil.ViewModels;

namespace ScanProMovil.Views.Products;

public partial class GestionProducts : ContentPage
{
	public GestionProducts(ProductsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    private void OnGoToDetailsProduct(object? sender, TappedEventArgs e) => UiGuard.Run(this, async () =>
    {
        if (sender is Border border && border.BindingContext is Product product)
        {
            await Navigation.PushAsync(new ProductDetails(product));
        }
    });
}