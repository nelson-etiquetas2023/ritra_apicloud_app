

namespace ScanProMovil.Views.Products;

public partial class ProductsPage : ContentPage
{
	public ProductsPage()
	{
		InitializeComponent();
	}

    private void LoadProductsRemote(object? sender, TappedEventArgs? e)
    {
        EffectClickTap(sender);

        var page = App.Current?.Handler.MauiContext?.
            Services.GetService<GestionProducts>();

        Navigation.PushAsync(page);
    }

    private void LocalDataProducts(object? sender, TappedEventArgs? e)
    {
        EffectClickTap(sender);
        var page = App.Current?.Handler.MauiContext?.
            Services.GetService<LocalDataProductsPage>();
        Navigation.PushAsync(page);
    }


    private void SincronizarData(object? sender, TappedEventArgs e)
    {
        EffectClickTap(sender);
    }

    private void CodeBarModule(object? sender, TappedEventArgs e)
    {
        EffectClickTap(sender);
    }
    private async void EffectClickTap(object? sender)
    {
        if (sender is Layout layout)
        {
            var originalColor = layout.Background;
            layout.Background = Colors.LimeGreen;
            await Task.Delay(50);
            layout.Background = originalColor;
        }
    }

  
}