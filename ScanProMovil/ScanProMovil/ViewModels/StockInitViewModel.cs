using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanProMovil.Entities;
using ScanProMovil.Services.StockInicial;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ScanProMovil.ViewModels
{
    /// <summary>
    /// Lista de conteos físicos (Stock Inicial). Réplica del patrón de
    /// <see cref="ComprasViewModel"/>: carga local, filtro por búsqueda y
    /// documento seleccionado sobre el que actúa el toolbar de la página.
    /// </summary>
    public partial class StockInitViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool refreshListDocs = true;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string loadingMessage = string.Empty;

        private List<StockInit> _allDocs = new();

        [ObservableProperty]
        private ObservableCollection<StockInit> docs = [];

        [ObservableProperty]
        private int totalDocs;

        [ObservableProperty]
        private StockInit? selectedDoc;

        private readonly IStockInitService _service;

        public StockInitViewModel(IStockInitService service)
        {
            _service = service;
        }

        [RelayCommand]
        public async Task GetDocsLocalSqlite()
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Cargando documentos...";

                var result = await _service.GetAllAsync();
                _allDocs = result;

                FilterDocs(SearchText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al obtener los documentos de stock inicial: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }

        private void FilterDocs(string text)
        {
            IEnumerable<StockInit> filtered;

            if (string.IsNullOrWhiteSpace(text))
            {
                filtered = _allDocs;
            }
            else
            {
                filtered = _allDocs.Where(d =>
                    (d.Numero?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.NumeroServidor?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Description?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Status?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    d.Fecha.ToString("dd-MM-yyyy HH:mm").Contains(text, StringComparison.OrdinalIgnoreCase));
            }

            Docs = new ObservableCollection<StockInit>(filtered);
            TotalDocs = Docs.Count;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterDocs(value);
        }
    }
}