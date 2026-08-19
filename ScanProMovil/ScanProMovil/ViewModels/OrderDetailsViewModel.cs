using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanProMovil.Data.Entities;

namespace ScanProMovil.ViewModels
{
    public partial class OrderDetailsViewModel : ObservableObject
    {

        [ObservableProperty]
        public bool isLoading; 

        [ObservableProperty]
        private Order order = new();

        public OrderDetailsViewModel(Order _order)
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
