using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScanProMovil.Entities
{
    /// <summary>
    /// Conteo físico capturado en el dispositivo. El vinculo con un inventario
    /// teórico se decide en la web, no aquí: por eso no guarda Doc. Teórico
    /// ni Doc. Físico.
    /// <para>
    /// <see cref="Numero"/> es la identidad local del conteo y se genera sin
    /// conexión como TMP-####. El correlativo oficial FIS-#### lo asigna el
    /// servidor al crear el documento y queda en <see cref="NumeroServidor"/>.
    /// Separarlos evita tener que re-clavear la clave primaria local y sus
    /// detalles cuando el servidor responde.
    /// </para>
    /// </summary>
    public class StockInit
    {
        public string Numero { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = null!;

        /// <summary>Correlativo FIS-#### asignado por el servidor. Null mientras no se sincroniza.</summary>
        public string? NumeroServidor { get; set; }

        [JsonIgnore]
        public List<StockItem> Items { get; set; } = new();

        [JsonIgnore]
        public int PendientesCount => Items?.Count(i => !i.Enviado) ?? 0;

        [JsonIgnore]
        public double TotalUnidades => Items?.Sum(i => i.Cantidad) ?? 0;

        /// <summary>Correlativo a mostrar: el del servidor cuando ya existe.</summary>
        [JsonIgnore]
        public string NumeroVisible => string.IsNullOrWhiteSpace(NumeroServidor) ? Numero : NumeroServidor;

        [JsonIgnore]
        public bool Sincronizado => !string.IsNullOrWhiteSpace(NumeroServidor);

        [JsonIgnore]
        public string ItemsSummary => Items is { Count: > 0 }
            ? $"{Items.Count} ítems / {TotalUnidades.ToString("N2")} und / {Items.Count - PendientesCount} sincronizados"
            : "0 ítems / 0 und / 0 sincronizados";
    }
}
