using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanProMovil.Data.Entities;

namespace ScanProMovil.ViewModels
{
    public partial class OrderConfigViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool selectMultipleRows;

        [ObservableProperty]
        private int maxOrdersProcess;

        public OrderConfigViewModel()
        {
            SelectMultipleRows = SettingsOrdersDefault.SelectMultipleRow;
            MaxOrdersProcess = SettingsOrdersDefault.MaxOrdersProcess;
        }

        [RelayCommand]
        public async Task SaveConfig() 
        {
            Preferences.Set("SelectMultipleRows", SelectMultipleRows);
            Preferences.Set("MaxOrdersProcess", MaxOrdersProcess);
        }
    }
}
