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
		// Device already initialized during login; use cached values
		EntryDeviceName.Text = _session.DeviceName;
		EntryWarehouseName.Text = _session.WarehouseName;
		EntryApiUrl.Text = Preferences.Get("ApiBaseUrl", "http://192.168.10.10:8080");
		LabelDeviceCode.Text = $" ({_session.DeviceCode})";
		LabelDeviceCode.IsVisible = !string.IsNullOrWhiteSpace(_session.DeviceCode);
		LabelMsg.IsVisible = false;
	}

	private void BtnSave_Clicked(object? sender, EventArgs? e)
		=> UiGuard.Run(this, async () =>
		{
			var deviceName = EntryDeviceName.Text?.Trim() ?? string.Empty;
			var warehouseName = EntryWarehouseName.Text?.Trim() ?? string.Empty;
			var apiUrl = EntryApiUrl.Text?.Trim().TrimEnd('/') ?? string.Empty;

			if (string.IsNullOrWhiteSpace(warehouseName))
			{
				LabelMsg.Text = "El nombre del almacén es obligatorio.";
				LabelMsg.TextColor = Colors.Red;
				LabelMsg.IsVisible = true;
				return;
			}

			if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri)
				|| (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
			{
				LabelMsg.Text = "La URL del servidor API no es válida (debe iniciar con http:// o https://).";
				LabelMsg.TextColor = Colors.Red;
				LabelMsg.IsVisible = true;
				return;
			}

			_session.DeviceName = deviceName;
			_session.WarehouseName = warehouseName;
			Preferences.Set("ApiBaseUrl", apiUrl);

			LabelMsg.Text = "Configuración guardada correctamente. Reinicia la aplicación para usar la nueva URL del API.";
			LabelMsg.TextColor = Colors.Green;
			LabelMsg.IsVisible = true;
		});
}