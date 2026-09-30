using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        if (item.HasChildren)
            return;

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

public class FlyoutMenuItem : INotifyPropertyChanged
{
    private List<FlyoutMenuItem> _children = [];
    private bool _isExpanded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public Type? TargetType { get; set; }
    public string? Message { get; set; }
    public Action<FlyoutPage>? Action { get; set; }

    public List<FlyoutMenuItem> Children
    {
        get => _children;
        set
        {
            _children = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChildren));
        }
    }

    public bool HasChildren => _children.Count > 0;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;

            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Nivel de anidamiento. 0 = raíz del menú, 1 = hijo de un grupo.</summary>
    public int IndentLevel { get; set; }

    /// <summary>Sangría visual según <see cref="IndentLevel"/>.</summary>
    public Thickness MenuPadding => new(20 + (IndentLevel * 22), 13);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}