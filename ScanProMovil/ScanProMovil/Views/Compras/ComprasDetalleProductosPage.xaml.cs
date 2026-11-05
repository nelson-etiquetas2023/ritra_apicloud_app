using ScanProMovil.Data.Entities;
using ScanProMovil.ViewModels;

namespace ScanProMovil.Views.Compras;

public partial class ComprasDetalleProductosPage : ContentPage
{
    private readonly ComprasDetalleProductosViewModel _vm;
    private bool _procesando;

    public ComprasDetalleProductosPage(OrdenCompra orden)
    {
        InitializeComponent();
        _vm = MauiProgram.Services!.GetRequiredService<ComprasDetalleProductosViewModel>();

        // La pagina se cierra sola cuando la orden queda Procesada en el
        // servidor: el VM invoca OnCompletado solo en el cierre completo.
        _vm.OnCompletado = async () =>
        {
            await Navigation.PopAsync();
        };

        _vm.Inicializar(orden);
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Seleccionar el entry de escaneo apenas entra al detalle. En
        // Android el foco no aplica si se pide durante la transicion:
        // se reintenta por unos ciclos hasta que queda enfocado.
        if (!_vm.EstaBloqueado)
        {
            _ = FocusearEscaneoAsync();
        }
    }

    private async Task FocusearEscaneoAsync()
    {
        for (var intento = 0; intento < 4 && !EntryCodigoBarra.IsFocused; intento++)
        {
            EntryCodigoBarra.Focus();
            await Task.Delay(150);
        }
    }

    /// <summary>Limpia el texto de un escáner Zebra (quita \r, \n, tabulaciones, espacios, asteriscos).</summary>
    private static string LimpiarCodigo(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var limpio = texto
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("\t", "")
            .Replace("*", "")
            .Trim();

        return limpio;
    }

    private async Task ProcesarCodigo(string codigoRaw)
    {
        if (_procesando) return;

        var codigo = LimpiarCodigo(codigoRaw);
        if (string.IsNullOrWhiteSpace(codigo)) return;

        _procesando = true;

        try
        {
            await _vm.ProcesarEscaneoCommand.ExecuteAsync(codigo);
        }
        finally
        {
            // Siempre limpiar el entry y devolver el foco
            EntryCodigoBarra.Text = string.Empty;
            EntryCodigoBarra?.Focus();
            _procesando = false;
        }
    }

    private void OnCodigoBarraTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_vm.EstaBloqueado) return;

        var texto = e.NewTextValue ?? string.Empty;
        var limpio = LimpiarCodigo(texto);

        // Si el texto tiene al menos 3 caracteres y no contiene asteriscos,
        // asumimos que es un código de barras completo y lo procesamos
        if (limpio.Length >= 3 && !texto.Contains('*'))
        {
            // Pequeño delay para asegurar que el escáner terminó de enviar
            _ = ProcesarConDebounce(texto);
        }
    }

    private async Task ProcesarConDebounce(string texto)
    {
        await Task.Delay(400);

        // Solo procesar si el texto no cambió durante el delay
        var textoActual = EntryCodigoBarra.Text ?? string.Empty;
        if (textoActual == texto)
        {
            await ProcesarCodigo(texto);
        }
    }

    private void OnCodigoBarraCompleted(object? sender, EventArgs e)
    {
        if (_vm.EstaBloqueado) return;

        var texto = EntryCodigoBarra.Text ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(texto))
        {
            _ = ProcesarCodigo(texto);
        }
    }
}
