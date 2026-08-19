using ScanProMovil.Services.StockInicial;
using StockInitEntity = ScanProMovil.Entities.StockInit;

namespace ScanProMovil.Views.StockInit;

public partial class CreateStockInitPage : ContentPage
{
    private readonly IStockInitService _service;
    private StockInitEntity? _doc;

    public CreateStockInitPage(IStockInitService service)
    {
        InitializeComponent();
        _service = service;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_doc is not null) return;

        var numero = await _service.GetNextNumberAsync();
        _doc = new StockInitEntity
        {
            Numero = numero,
            Fecha = DateTime.Today,
            Document_Teorico = "DT-000",
            Description = string.Empty,
            Status = "Iniciado",
            Items = new System.Collections.Generic.List<ScanProMovil.Entities.StockItem>()
        };

        NumeroLabel.Text = numero;
        FechaPicker.Date = DateTime.Today;
        TeoricoEntry.Text = _doc.Document_Teorico;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_doc is null) return;

        _doc.Fecha = FechaPicker.Date ?? DateTime.Today;
        _doc.Document_Teorico = string.IsNullOrWhiteSpace(TeoricoEntry.Text) ? "DT-000" : TeoricoEntry.Text.Trim();
        _doc.Description = DescriptionEditor.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_doc.Description))
        {
            await DisplayAlertAsync("Validación", "Ingrese una descripción para el documento.", "OK");
            return;
        }

        SavingOverlay.IsVisible = true;
        SavingIndicator.IsRunning = true;
        SavingIndicator.IsVisible = true;
        BtnSave.IsEnabled = false;

        var saveTask = _service.CreateAsync(_doc);
        await Task.WhenAll(saveTask, Task.Delay(400));
        var ok = await saveTask;

        SavingIndicator.IsRunning = false;
        SavingIndicator.IsVisible = false;
        SavingOverlay.IsVisible = false;
        BtnSave.IsEnabled = true;

        if (ok)
            await Navigation.PopAsync();
        else
            await DisplayAlertAsync("Error", "No se pudo crear el documento.", "OK");
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}