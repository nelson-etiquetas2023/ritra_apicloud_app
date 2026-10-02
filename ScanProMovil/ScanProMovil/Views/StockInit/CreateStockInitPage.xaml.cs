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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UiGuard.Run(this, async () =>
        {
            if (_doc is not null) return;

            var numero = await _service.GetNextNumberAsync();
            _doc = new StockInitEntity
            {
                Numero = numero,
                Fecha = DateTime.Today,
                Description = string.Empty,
                Status = "Iniciado",
                Items = new System.Collections.Generic.List<ScanProMovil.Entities.StockItem>()
            };

            NumeroLabel.Text = numero;
            FechaPicker.Date = DateTime.Today;
        });
    }

    private void OnSaveClicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        if (_doc is null) return;

        _doc.Fecha = FechaPicker.Date ?? DateTime.Today;
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

        try
        {
            var saveTask = _service.CreateAsync(_doc);
            await Task.WhenAll(saveTask, Task.Delay(400));
            var ok = await saveTask;

            if (ok)
                await Navigation.PopAsync();
            else
                await DisplayAlertAsync("Error", "No se pudo crear el documento.", "OK");
        }
        finally
        {
            // Overlay se oculta siempre: si falla, la pantalla no queda congelada.
            SavingIndicator.IsRunning = false;
            SavingIndicator.IsVisible = false;
            SavingOverlay.IsVisible = false;
            BtnSave.IsEnabled = true;
        }
    });

    private void OnCancelClicked(object? sender, EventArgs e)
        => UiGuard.Run(this, () => Navigation.PopAsync());
}