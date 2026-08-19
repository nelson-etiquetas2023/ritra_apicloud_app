using ScanProMovil.Data.Entities;
using ScanProMovil.ViewModels;
using System.Diagnostics;

namespace ScanProMovil.Views.Compras;

public partial class AddComprasPage : ContentPage
{
	public readonly AddComprasViewModels _vm;
	public Int32 totalrows = 0;
    public double totcantidad = 0;

    public AddComprasPage(AddComprasViewModels vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = _vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadNextOrderNumberAsync();
    }

    void UpdateGrandTotal()
    {
        totcantidad = _vm.NewOrder.Items.Sum(x => x.Cantidad);
        double totorder = _vm.NewOrder.Items.Sum(x => x.Cantidad * x.Costo);
        totalrows = _vm.NewOrder.Items.Count();
        lbl_total_renglon.Text = totalrows.ToString();
        lbl_total_cant.Text = totcantidad.ToString();
        lbl_totalCosto.Text = totorder.ToString("$#,##0.00");
        _vm.CalcularTotales();
    }

    private async void btn_AddProducts_Clicked(object? sender, EventArgs? e)
    {
        if (_vm.ProductScan is null)
        {
            await DisplayAlertAsync("Producto no encontrado",
                "No se puede agregar un producto que no existe en la base de datos...", "OK");
            return;
        }

        if (!double.TryParse(txtQuantityEntry.Text, out double cantidad) || cantidad < 1)
        {
            await DisplayAlertAsync("Validar Cantidad",
                "Debe ingresar en cantidad un valor mayor 0...", "OK");
            txtQuantityEntry.Text = string.Empty;
            txtQuantityEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtProductIdEntry.Text.Trim()))
        {
            await DisplayAlertAsync("validation", "Enter Product id...", "Ok");
            return;
        }

        var item = new DetalleCompra()
        {
            Product_id = txtProductIdEntry.Text.Trim(),
            Product_Name = txt_NameProducts.Text.Trim(),
            Cantidad = cantidad,
            Numero = _vm.NewOrder.Numero,
            Costo = _vm.ProductScan!.Costo,
            Subtotal = cantidad * _vm.ProductScan!.Costo
        };
        _vm.NewOrder.ItemsNumber = totalrows + 1;
        _vm.NewOrder.Items.Add(item);
        txtProductIdEntry.Text = string.Empty;
        txtQuantityEntry.Text = string.Empty;
        txt_NameProducts.Text = string.Empty;
        _vm.ProductScan = null;
        btnAddProduct.IsEnabled = false;

        txtProductIdEntry.Focus();
        UpdateGrandTotal();
    }

    private async void OnEditItem(object? sender, EventArgs e)
    {
        if (sender is not ImageButton btn || btn.CommandParameter is not DetalleCompra item)
            return;

        var cantidad = await DisplayPromptAsync("Editar Cantidad",
            $"Producto: {item.Product_Name}",
            "OK", "Cancelar", "Ingrese la cantidad",
            maxLength: 10, keyboard: Keyboard.Numeric);

        if (!double.TryParse(cantidad, out var nuevaCantidad) || nuevaCantidad < 1)
        {
            await DisplayAlertAsync("Validar Cantidad",
                "Debe ingresar en cantidad un valor mayor a 0.", "OK");
            return;
        }

        item.Cantidad = nuevaCantidad;
        item.Subtotal = nuevaCantidad * item.Costo;
        _vm.NewOrder.ItemsNumber = _vm.NewOrder.Items.Count;
        UpdateGrandTotal();
    }

    private void OnRemoveItem(object? sender, EventArgs e)
    {
        if (sender is not ImageButton btn || btn.CommandParameter is not DetalleCompra item)
            return;

        _vm.NewOrder.Items.Remove(item);
        _vm.NewOrder.ItemsNumber = _vm.NewOrder.Items.Count;
        UpdateGrandTotal();
    }

    private async void Btn_Save_Order_Clicked(object? sender, EventArgs? e)
    {
        if (_vm.NewOrder.Items.Count == 0)
        {
            await DisplayAlertAsync("Sin productos",
                "Debe agregar al menos un producto antes de guardar.", "OK");
            return;
        }

        SavingIndicator.IsRunning = true;
        SavingIndicator.IsVisible = true;
        SavingLabel.IsVisible = true;

        var saved = await _vm.SaveOrderLocalSqliteAsync();

        SavingIndicator.IsRunning = false;
        SavingIndicator.IsVisible = false;
        SavingLabel.IsVisible = false;

        if (saved)
        {
            _vm.Ordenes.Add(_vm.NewOrder);
            await DisplayAlertAsync("Guardado",
                $"El documento {_vm.NewOrder.Numero} fue guardado correctamente.", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlertAsync("Error",
                "No se pudo guardar el documento. Intente nuevamente.", "OK");
        }
    }

    private async void Btn_Cancel_Order_Clicked(object? sender, EventArgs? e)
    {
        await Navigation.PopAsync();
    }

    private async void SearchSupplyData(object? sender, EventArgs? e)
    {
        string option = await DisplayActionSheetAsync("Seleccion un proveedor:","Cancelar"
            ,null,"Supply Santo Domingo","Todo Express","Supply-Todo");

        switch (option) 
        {
            case "Supply Santo Domingo":
                txt_supplyName.Text = "Supply Santo Domingo";
                break;   
            case "Todo Express":
                txt_supplyName.Text = "Todo Express";
                break;
            case "Supply-Todo":
                txt_supplyName.Text = "Supply-Todo";
                break;
        }
    }

    private async void txtProductIdEntry_TextChanged(object? sender, TextChangedEventArgs? e)
    {
        //busqueda solo codigo de barra
        var text = e!.NewTextValue;
        if (string.IsNullOrWhiteSpace(text)) return;
        //verifico si es un codigo de barra.
        if (text.StartsWith('*') && text.EndsWith('*'))
        {
            Debug.WriteLine("valor de Codigo de Barra:" + text);
            var textClear = text.Trim("*").ToString();
            _vm.SearchText = textClear;
            await _vm.GetProductLocalByIdCommand.ExecuteAsync(null);
            txtProductIdEntry.Text = textClear;

            if (_vm.ProductScan is not null)
            {
                txt_NameProducts.Text = _vm.ProductScan.Product_Name;
                btnAddProduct.IsEnabled = true;
                txtQuantityEntry.Focus();
            }
            else
            {
                txt_NameProducts.Text = string.Empty;
                txtQuantityEntry.Text = string.Empty;
                btnAddProduct.IsEnabled = false;

                await DisplayAlertAsync("Producto no encontrado",
                    $"El producto {textClear} no existe en la base de datos...", "OK");

                //blanquear los datos para escanear otro producto que exista.
                txtProductIdEntry.Text = string.Empty;
                txt_NameProducts.Text = string.Empty;
                txtQuantityEntry.Text = string.Empty;
                btnAddProduct.IsEnabled = false;
                txtProductIdEntry.Focus();
            }
        }
    }
}