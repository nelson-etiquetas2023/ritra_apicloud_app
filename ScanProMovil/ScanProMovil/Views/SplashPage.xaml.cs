namespace ScanProMovil.Views;

public partial class SplashPage : ContentPage
{
    private CancellationTokenSource? _dotsCts;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _dotsCts = new CancellationTokenSource();
        _ = AnimateDotsAsync(_dotsCts.Token);

        await Task.Delay(2200);

        _dotsCts.Cancel();

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is not null)
            window.Page = new NavigationPage(new LoginPage());
    }

    private async Task AnimateDotsAsync(CancellationToken ct)
    {
        var dots = new VisualElement[] { Dot1, Dot2, Dot3 };
        try
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (var dot in dots)
                {
                    await dot.ScaleToAsync(0.6, 150, Easing.CubicIn);
                    await dot.ScaleToAsync(1.2, 150, Easing.CubicOut);
                    await dot.ScaleToAsync(1.0, 150, Easing.CubicOut);
                }
                await Task.Delay(200, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}