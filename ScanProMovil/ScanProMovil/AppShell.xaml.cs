using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls;

namespace ScanProMovil
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Aquí puedes registrar rutas para navegación con Shell
            // Por ejemplo, una página de detalle de producto:
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        }

    }
}
