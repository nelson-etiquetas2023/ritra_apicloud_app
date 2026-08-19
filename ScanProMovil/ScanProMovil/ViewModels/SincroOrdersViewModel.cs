
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ScanProMovil.ViewModels
{
    public partial class SincroOrdersViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<string> logs=[];

        [ObservableProperty]
        bool connectionInternet;

        [ObservableProperty]
        bool isSincronizando;

        public bool IsSincronizarEnabled => !IsSincronizando;

        partial void OnIsSincronizandoChanged(bool value)
        {
            OnPropertyChanged(nameof(IsSincronizarEnabled));
        }

        public SincroOrdersViewModel()
        {
            Logs.Clear();  
        }

        [RelayCommand]
        public async Task ExecuteTestSincro() 
        {
            IsSincronizando = true;
            Logs.Clear();
            await Task.Delay(1000);
            Logs.Add("Iniciando sincronización...");
            await Task.Delay(1000);
            Logs.Add("Verificando la conexion a internet...");
            await Task.Delay(1000);
            Logs.Add("Ordenes a sincronizar 3 => [1250,5001,5824]");
            await Task.Delay(1000);
            Logs.Add("conectando con el servidor...");
            await Task.Delay(1000);
            Logs.Add("server ok...");
            await Task.Delay(1000);
            Logs.Add("transfirieno orden num. 1250...");
            await Task.Delay(1000);
            Logs.Add("transfirieno orden num. 5001...");
            await Task.Delay(1000);
            Logs.Add("transfirieno orden num. 5824...");
            await Task.Delay(1000);
            Logs.Add("se transfirieron correctamente 3 ordenes");
            await Task.Delay(1000);
            Logs.Add("0 errores ocurridos...");
            await Task.Delay(1000);
            Logs.Add("cerrando conexion");
            await Task.Delay(1000);
            Logs.Add("duacion 2 minutos con 3 segundos");
            await Task.Delay(1000);
            Logs.Add("proceso completado.");
        }
    }
}
