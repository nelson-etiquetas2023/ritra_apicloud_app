using CommunityToolkit.Maui.Views;

namespace ScanProMovil.Views.StockInit;

public partial class QuantityPopup : Popup
{
    public double Cantidad { get; private set; }

    public QuantityPopup(string productName)
    {
        InitializeComponent();
        ProductNameLabel.Text = productName;
        Opened += (_, _) =>
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () => QuantityEntry.Focus());
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        if (double.TryParse(QuantityEntry.Text, out var cantidad) && cantidad > 0)
        {
            Cantidad = cantidad;
            await CloseAsync();
        }
        else
        {
            ErrorLabel.Text = "Ingrese una cantidad mayor a 0.";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CloseAsync();
    }

    private void OnMinusClicked(object? sender, EventArgs e)
    {
        if (double.TryParse(QuantityEntry.Text, out var cantidad))
        {
            cantidad -= 1;
            QuantityEntry.Text = cantidad > 0 ? cantidad.ToString("0.##") : "1";
            QuantityEntry.CursorPosition = QuantityEntry.Text.Length;
        }
    }

    private void OnPlusClicked(object? sender, EventArgs e)
    {
        if (double.TryParse(QuantityEntry.Text, out var cantidad))
        {
            QuantityEntry.Text = (cantidad + 1).ToString("0.##");
            QuantityEntry.CursorPosition = QuantityEntry.Text.Length;
        }
        else
        {
            QuantityEntry.Text = "1";
        }
    }
}