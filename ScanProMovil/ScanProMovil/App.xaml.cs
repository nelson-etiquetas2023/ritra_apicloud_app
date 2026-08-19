using Microsoft.Extensions.DependencyInjection;
using ScanProMovil.Views;

namespace ScanProMovil
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new SplashPage());
        }
    }
}