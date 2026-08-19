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
}