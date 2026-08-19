using Microsoft.Maui.Dispatching;
using ScanProMovil.Services.Auth;

namespace ScanProMovil.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly IAuthService _authService;
        private readonly AuthSession _session;

        public LoginPage()
        {
            InitializeComponent();
            _authService = MauiProgram.Services!.GetRequiredService<IAuthService>();
            _session = MauiProgram.Services!.GetRequiredService<AuthSession>();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            EntryEmail.Text = string.Empty;
            EntryPassword.Text = string.Empty;
            LabelMessage.Text = string.Empty;
            LabelAttempts.TextColor = Colors.Gray;
            UpdateScreen();

            if (_session.IsBlocked)
            {
                StartBlockCountdown();
                return;
            }

            ShowVirtualKeyboard();
        }

        private void ShowVirtualKeyboard()
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(250);
                Dispatcher.Dispatch(() =>
                {
                    if (!_session.IsBlocked)
                        EntryEmail.Focus();
                });
            });
        }

        private bool _isPasswordVisible;

        private void OnTogglePassword(object? sender, TappedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            EntryPassword.IsPassword = !_isPasswordVisible;
            ImageTogglePassword.Source = _isPasswordVisible ? "eye_slash.svg" : "eye.svg";
            EntryPassword.Focus();
        }

        private void OnEmailCompleted(object? sender, EventArgs e)
        {
            EntryPassword.Focus();
        }

        private void OnPasswordCompleted(object? sender, EventArgs e)
        {
            if (BtnLogin.IsEnabled)
                BtnLogin_Clicked(sender, e);
        }

        private void UpdateScreen()
        {
            if (_session.IsBlocked)
            {
                LoginForm.IsVisible = false;
                BlockView.IsVisible = true;
                UpdateBlockCountdown();
            }
            else
            {
                BlockView.IsVisible = false;
                LoginForm.IsVisible = true;
                var remaining = AuthSession.MaxAttempts - _session.FailedAttempts;
                LabelAttempts.Text = remaining > 0
                    ? $"Intentos restantes: {remaining}"
                    : string.Empty;
            }
        }

        private void UpdateBlockCountdown()
        {
            if (_session.BlockedUntil is not { } until) return;
            var diff = until - DateTimeOffset.Now;
            if (diff < TimeSpan.Zero) diff = TimeSpan.Zero;
            LabelBlockCountdown.Text =
                $"El sistema se desbloqueará en {diff.Minutes} min {diff.Seconds} seg.";
        }

        private void StartBlockCountdown()
        {
            Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (!_session.IsBlocked)
                {
                    _session.Reset();
                    MainThread.BeginInvokeOnMainThread(UpdateScreen);
                    return false;
                }
                MainThread.BeginInvokeOnMainThread(UpdateBlockCountdown);
                return true;
            });
        }

        private async void BtnLogin_Clicked(object? sender, EventArgs e)
        {
            var email = EntryEmail.Text?.Trim() ?? string.Empty;
            var password = EntryPassword.Text ?? string.Empty;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlertAsync("Datos incompletos",
                    "Ingrese correo y contraseña.", "OK");
                return;
            }

            if (_session.IsBlocked)
            {
                await DisplayAlertAsync("Sistema bloqueado",
                    "El sistema está bloqueado. Intente nuevamente dentro de unos minutos.", "OK");
                return;
            }

            BtnLogin.IsEnabled = false;
            SetLoading(true);

            LoginResult result;
            try
            {
                result = await _authService.LoginAsync(email, password);
            }
            catch (Exception ex)
            {
                result = new LoginResult
                {
                    Success = false,
                    Message = "No se pudo conectar con el servidor: " + ex.Message
                };
            }

            SetLoading(false);

            if (result.Success)
            {
                await NavigateToMain();
                return;
            }

            BtnLogin.IsEnabled = true;
            LabelMessage.TextColor = Colors.Red;

            _session.FailedAttempts += 1;
            var remaining = AuthSession.MaxAttempts - _session.FailedAttempts;

            if (remaining <= 0)
            {
                _session.FailedAttempts = 0;
                _session.BlockedUntil = DateTimeOffset.Now.Add(_session.LockDuration);
                LabelMessage.Text = string.Empty;
                UpdateScreen();
                StartBlockCountdown();

                await DisplayAlertAsync("Sistema bloqueado",
                    "Demasiados intentos fallidos. El sistema queda bloqueado por " +
                    $"{(int)_session.LockDuration.TotalMinutes} minutos.", "OK");
            }
            else
            {
                LabelMessage.Text = string.Empty;
                var pendingMessage = $"Intentos restantes: {remaining}";
                LabelAttempts.Text = pendingMessage;

                await DisplayAlertAsync("Error de acceso", result.Message, "OK");
                await DisplayAlertAsync("Intento fallido", pendingMessage, "OK");
            }
        }

        private void SetLoading(bool isLoading)
        {
            LoadingView.IsVisible = isLoading;
            LoginIndicator.IsRunning = isLoading;
            LoginIndicator.IsVisible = isLoading;
            BtnLogin.IsEnabled = !isLoading;
        }

        private async Task NavigateToMain()
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window is not null)
            {
                window.Page = new FlyoutMainPage();
                return;
            }
            await Navigation.PushAsync(new FlyoutMainPage());
        }
    }
}