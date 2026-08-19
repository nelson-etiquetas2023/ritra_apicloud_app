using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanProMovil.Data.Entities;

namespace ScanProMovil.ViewModels
{
    public partial class DetailsComprasViewModels : ObservableObject
    {
        [ObservableProperty]
        public bool isLoading;

        [ObservableProperty]
        private OrdenCompra order = new();

        public DetailsComprasViewModels(OrdenCompra _order)
        {
            order = _order;
        }

        [RelayCommand]
        public void PrintOrder()
        {

        }

        [RelayCommand]
        public void DownloadOrder()
        {

        }
    }
}
