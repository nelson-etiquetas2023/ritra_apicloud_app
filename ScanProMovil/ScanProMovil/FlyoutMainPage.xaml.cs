using ScanProMovil.Views;

namespace ScanProMovil;

public partial class FlyoutMainPage : FlyoutPage
{
    public FlyoutMainPage()
    {
        InitializeComponent();
        FlyoutMenuHost.Attach(this);
    }
}