using ScanProMovil.Services.Session;

namespace ScanProMovil.Views;

public partial class SettingPage : ContentPage
{
	private readonly AppSession _session;

	public SettingPage()
	{
		InitializeComponent();
		_session = MauiProgram.Services!.GetRequiredService<AppSession>();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		LoadCurrentValues();
	}

	private void LoadCurrentValues()
	{
		_session.InitializeDevice();
		EntryDeviceName.Text = _session.DeviceName;
		EntryWarehouseName.Text = _session.WarehouseName;
		LabelDeviceCode.Text = $" ({_session.DeviceCode})";
		LabelDeviceCode.IsVisible = !string.IsNullOrWhiteSpace(_session.DeviceCode);
		LabelMsg.IsVisible = false;
	}

	private async void BtnSave_Clicked(object? sender, EventArgs? e)
	{
		var deviceName = EntryDeviceName.Text?.Trim() ?? string.Empty;
		var warehouseName = EntryWarehouseName.Text?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(warehouseName))
		{
			LabelMsg.Text = "El nombre del almacén es obligatorio.";
			LabelMsg.TextColor = Colors.Red;
			LabelMsg.IsVisible = true;
			return;
		}

		_session.DeviceName = deviceName;
		_session.WarehouseName = warehouseName;

		LabelMsg.Text = "Configuración guardada correctamente.";
		LabelMsg.TextColor = Colors.Green;
		LabelMsg.IsVisible = true;
	}
}