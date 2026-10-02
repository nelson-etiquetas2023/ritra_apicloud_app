using ScanProMovil.Data.Entities;
using ScanProMovil.ViewModels;


namespace ScanProMovil.Views;

public partial class OrdersPage : ContentPage
{
    private readonly OrderViewModel _vm;
    private Int32 totalrows = 0;

    public OrdersPage(OrderViewModel vm)
	{
		InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    void UpdateGrandTotal() 
    {
        double totcantidad = _vm.NewOrder.Items.Sum(x => x.Cantidad);
        totalrows = _vm.NewOrder.Items.Count();
        TotalCantEntry.Text = Convert.ToString(totcantidad);
        TotalRowsEntry.Text = Convert.ToString(totalrows);
    }

    public void btn_AddProducts_Clicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (string.IsNullOrWhiteSpace(txtProductIdEntry.Text?.Trim()))
        {
            await DisplayAlertAsync("validation", "Enter Product id...", "Ok");
            return;
        }

        // Validación explícita: Convert.ToDouble lanzaba FormatException con texto vacío/no numérico.
        if (!double.TryParse(txtQuantityEntry.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var cantidad) || cantidad <= 0)
        {
            await DisplayAlertAsync("validation", "Ingrese una cantidad válida...", "Ok");
            return;
        }

        var item = new OrderDetails()
        {
            productId = txtProductIdEntry.Text.Trim(),
            Cantidad = cantidad,
            OrderNumber = _vm.NewOrder.OrderNumber
        };
        _vm.NewOrder.ItemsNumber = totalrows + 1;
        _vm.NewOrder.Items.Add(item);
        txtProductIdEntry.Text = string.Empty;
        txtQuantityEntry.Text = string.Empty;
        txtProductIdEntry.Focus();
        UpdateGrandTotal();
    });

    private void btn_RemoveProducts_Clicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is OrderDetails item) 
        {
            _vm.NewOrder.Items.Remove(item);
            UpdateGrandTotal();
            
        }

    }

    private void Btn_Save_Order_Clicked(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        SavingIndicator.IsRunning = true;
        SavingIndicator.IsVisible = true;
        SavingLabel.IsVisible = true;

        try
        {
            await Task.Delay(1000);
            //guardar la orden de manera local en el dispositvo en sqlite
            await _vm.SaveOrderLocalSqliteCommand.ExecuteAsync(null);

            //actualizar la lista de ordebes
            _vm.Ordenes.Add(_vm.NewOrder);

            //navega de vuelta al index de orders
            await Navigation.PopAsync();
        }
        finally
        {
            // Si el guardado falla, los indicadores se apagan igual (pantalla no bloqueada).
            SavingIndicator.IsRunning = false;
            SavingIndicator.IsVisible = false;
            SavingLabel.IsVisible = false;
        }
    });
}