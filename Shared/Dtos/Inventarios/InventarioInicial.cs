using System.Text.Json.Serialization;

namespace Shared.Dtos.Inventarios
{
    /// <summary>
    /// Comparación de solo lectura entre un documento teórico (web) y uno físico (móvil).
    /// No escribe en base de datos.
    /// </summary>
    public class ComparacionRequest
    {
        public string DocumentoTeorico { get; set; } = "";
        public string InventarioFisico { get; set; } = "";
    }

    public class ComparacionDetalle
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";

        public double CantidadTeorica { get; set; }
        public double CantidadFisica { get; set; }

        [JsonIgnore]
        public double Diferencia => CantidadFisica - CantidadTeorica;

        /// <summary>Está en el teórico pero no se escaneó: al aplicar el físico se pondrá en 0.</summary>
        public bool SoloTeorico { get; set; }

        /// <summary>Se escaneó pero no está en el teórico conciliado.</summary>
        public bool SoloFisico { get; set; }

        public string Ubicacion { get; set; } = "";
    }

    public class ComparacionResult
    {
        public string DocumentoTeorico { get; set; } = "";
        public string InventarioFisico { get; set; } = "";
        public List<ComparacionDetalle> Detalles { get; set; } = [];

        [JsonIgnore]
        public double TotalTeorico => Detalles.Sum(d => d.CantidadTeorica);

        [JsonIgnore]
        public double TotalFisico => Detalles.Sum(d => d.CantidadFisica);

        [JsonIgnore]
        public double TotalDiferencia => TotalFisico - TotalTeorico;

        [JsonIgnore]
        public int ItemsCoinciden => Detalles.Count(d => Math.Abs(d.Diferencia) < 0.0001);

        [JsonIgnore]
        public int ItemsConDiferencia => Detalles.Count(d => Math.Abs(d.Diferencia) >= 0.0001);

        [JsonIgnore]
        public int ItemsSoloTeorico => Detalles.Count(d => d.SoloTeorico);

        [JsonIgnore]
        public int ItemsSoloFisico => Detalles.Count(d => d.SoloFisico);
    }

    /// <summary>
    /// Aplica el documento físico al inventario. Si DocumentoTeorico viene vacío,
    /// la aplicación es "solo físico" y solo se tocan los productos escaneados.
    /// </summary>
    public class AplicarFisicoRequest
    {
        public string DocumentoTeorico { get; set; } = "";
    }

    public class AplicacionLinea
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public double CantidadFisica { get; set; }
        public double StockAnterior { get; set; }
        public double StockNuevo { get; set; }

        /// <summary>Estaba en el teórico conciliado y no se escaneó: su stock quedó en 0.</summary>
        public bool PuestoEnCero { get; set; }
    }

    public class AplicarFisicoResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string InventarioFisico { get; set; } = "";
        public string DocumentoTeorico { get; set; } = "";
        public DateTime? FechaAplicacion { get; set; }
        public int ProductosActualizados { get; set; }
        public int ProductosPuestosEnCero { get; set; }
        public List<AplicacionLinea> Lineas { get; set; } = [];
        public List<RowError> Errors { get; set; } = [];
    }
}
