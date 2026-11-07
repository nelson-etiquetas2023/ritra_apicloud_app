namespace Shared.Dtos.Inventario
{
    /// <summary>
    /// Solicitud para registrar un movimiento manual de inventario de un producto.
    /// Inicial: existencia base. Entradas/Salidas: movimientos a aplicar.
    /// Existencia se calcula como Inicial + Entradas - Salidas.
    /// </summary>
    public class RegistroMovimientoRequest
    {
        public string CodigoProducto { get; set; } = "";
        public string NombreProducto { get; set; } = "";
        public double Inicial { get; set; }
        public double Entradas { get; set; }
        public double Salidas { get; set; }
        public double Existencia => Inicial + Entradas - Salidas;
        public string Motivo { get; set; } = "Ajuste manual";
        public string Usuario { get; set; } = "";
        public string Comentario { get; set; } = "";
    }
}
