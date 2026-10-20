using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Dtos.Turnos
{
    public static class TurnoStatus
    {
        public const int Abierto = 0;
        public const int Cerrado = 1;
    }

    public static class TurnoMovimientoTipo
    {
        public const string Entrada = "Entrada";
        public const string Salida = "Salida";
    }

    // Denominaciones DOP: billetes 2000..50, monedas 25..1.
    public static class DenominacionesDOP
    {
        public static readonly decimal[] Todas = [2000, 1000, 500, 200, 100, 50, 25, 10, 5, 1];
        public static string Etiqueta(decimal d) => d >= 50 ? $"${d:N0}" : $"${d:N0} M";
    }

    public class TurnoCaja
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Caja { get; set; } = "Caja 1";

        [Required]
        [StringLength(100)]
        public string Cajero { get; set; } = "";

        public DateTime FechaApertura { get; set; } = DateTime.Now;

        public decimal FondoInicial { get; set; }

        public DateTime? FechaCierre { get; set; }

        public int Status { get; set; } = TurnoStatus.Abierto;

        [StringLength(250)]
        public string Observacion { get; set; } = "";

        // Snapshot del cuadre al cerrar (auditoría).
        public decimal Esperado { get; set; }
        public decimal Contado { get; set; }
        public decimal Diferencia { get; set; }

        [JsonIgnore]
        public string StatusTexto => Status switch
        {
            TurnoStatus.Abierto => "Abierto",
            TurnoStatus.Cerrado => "Cerrado",
            _ => Status.ToString()
        };
    }

    public class TurnoDenominacion
    {
        [Key]
        public int Id { get; set; }

        public int TurnoId { get; set; }

        public decimal Denominacion { get; set; }

        public int Cantidad { get; set; }

        [JsonIgnore]
        public decimal Importe => Denominacion * Cantidad;
    }

    public class TurnoMovimiento
    {
        [Key]
        public int Id { get; set; }

        public int TurnoId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [StringLength(10)]
        public string Tipo { get; set; } = TurnoMovimientoTipo.Salida;

        public decimal Monto { get; set; }

        [Required]
        [StringLength(250)]
        public string Motivo { get; set; } = "";
    }

    public class ConteoDenominacion
    {
        public decimal Denominacion { get; set; }
        public int Cantidad { get; set; }
    }

    public class AbrirTurnoRequest
    {
        public string Caja { get; set; } = "Caja 1";
        public string Cajero { get; set; } = "";
        public decimal FondoInicial { get; set; }
    }

    public class TurnoMovimientoRequest
    {
        public int TurnoId { get; set; }
        public string Tipo { get; set; } = TurnoMovimientoTipo.Salida;
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = "";
    }

    public class CerrarTurnoRequest
    {
        public List<ConteoDenominacion> Conteos { get; set; } = [];
        public string Observacion { get; set; } = "";
        public string CerradoPor { get; set; } = "";
    }

    public class TurnoResumen
    {
        public int TurnoId { get; set; }
        public decimal FondoInicial { get; set; }
        public int VentasProcesadas { get; set; }
        public decimal VentasEfectivo { get; set; }
        public int FacturasAnuladas { get; set; }
        public decimal MontoAnulado { get; set; }
        public decimal VentaNeta { get; set; }
        public decimal VentasElectronico { get; set; }
        public decimal Entradas { get; set; }
        public decimal Salidas { get; set; }
        public decimal Esperado { get; set; }
    }

    public class TurnoSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public TurnoCaja? Data { get; set; }
    }

    public class TurnoCierreResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public TurnoCaja? Turno { get; set; }
        public List<TurnoDenominacion> Conteos { get; set; } = [];
    }
}
