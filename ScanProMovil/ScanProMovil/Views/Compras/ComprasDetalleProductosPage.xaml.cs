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

        // Al cerrar el dialogo de confirmacion, devolver el foco al escaner
        // para modo continuo (scan-and-go).
        _vm.OnDialogoCerrado = () =>
        {
            EntryCodigoBarra.Focus();
        };

        // Al abrir el dialogo de confirmacion, dar el foco al entry de escaneo del dialogo.
        _vm.OnDialogoAbierto = () =>
        {
            _ = FocusearEntryDialogoAsync();
        };

        _vm.Inicializar(orden);
        BindingContext = _vm;
    }

    private async Task FocusearEntryDialogoAsync()
    {
        for (var intento = 0; intento < 4 && !EntryCodigoBarraDialogo.IsFocused; intento++)
        {
            EntryCodigoBarraDialogo.Focus();
            await Task.Delay(150);
        }
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

    private void OnCodigoBarraDialogoCompleted(object? sender, EventArgs e)
    {
        var entry = (Entry)sender;
        var texto = entry.Text ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(texto))
        {
            _ = _vm.ProcesarEscaneoDialogoCommand.ExecuteAsync(texto);
        }
        // Borrar el entry para escanear el siguiente código
        entry.Text = string.Empty;
    }

    private void OnCodigoBarraDialogoTextChanged(object? sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue ?? string.Empty;
        var limpio = LimpiarCodigo(texto);

        // Si el texto tiene al menos 3 caracteres y no contiene asteriscos,
        // asumimos que es un código de barras completo y lo procesamos
        if (limpio.Length >= 3 && !texto.Contains('*'))
        {
            _ = ProcesarConDebounceDialogo(texto);
        }
    }

    private async Task ProcesarConDebounceDialogo(string texto)
    {
        await Task.Delay(400);

        // Solo procesar si el texto no cambió durante el delay
        var textoActual = EntryCodigoBarraDialogo.Text ?? string.Empty;
        if (textoActual == texto)
        {
            await ProcesarCodigoDialogo(texto);
        }
    }

    private async Task ProcesarCodigoDialogo(string codigoRaw)
    {
        if (_procesando) return;

        var codigo = LimpiarCodigo(codigoRaw);
        if (string.IsNullOrWhiteSpace(codigo)) return;

        _procesando = true;

        try
        {
            await _vm.ProcesarEscaneoDialogoCommand.ExecuteAsync(codigo);
        }
        finally
        {
            // Borrar el entry para escanear el siguiente código
            EntryCodigoBarraDialogo.Text = string.Empty;
            _procesando = false;
        }
    }
}
