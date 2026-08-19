using ScanProMovil.Services.Session;

namespace ScanProMovil.Views;

public static class FlyoutMenuHost
{
    public static void Attach(FlyoutPage page, IReadOnlyList<FlyoutMenuItem>? topItems = null, bool includeDefaults = true)
    {
        var session = MauiProgram.Services!.GetRequiredService<AppSession>();

        var menu = new FlyoutMenuPage(topItems, includeDefaults);
        menu.MenuItemSelected += async (_, item) => await NavigateToAsync(page, item);
        menu.LogoutRequested += (_, _) => Logout(session);
        page.Flyout = menu;
    }

    private static async Task NavigateToAsync(FlyoutPage page, FlyoutMenuItem item)
    {
        page.IsPresented = false;

        if (item.TargetType is null)
        {
            if (item.Action is not null)
            {
                item.Action(page);
                return;
            }

            if (!string.IsNullOrWhiteSpace(item.Message) && page.Detail is Page detail)
                await detail.DisplayAlertAsync(item.Title, item.Message, "OK");
            return;
        }

        var target = (MauiProgram.Services?.GetService(item.TargetType) as Page)
            ?? (Page)Activator.CreateInstance(item.TargetType)!;

        if (page.Detail is NavigationPage nav)
            await nav.PushAsync(target);
    }

    private static void Logout(AppSession session)
    {
        session.Logout();
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is not null)
            window.Page = new NavigationPage(new LoginPage());
    }
}

public class FlyoutMenuItem
{
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public Type? TargetType { get; set; }
    public string? Message { get; set; }
    public Action<FlyoutPage>? Action { get; set; }
}