namespace Shared.Dtos.Compras
{
    public class ProcesarOrdenResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public long ElapsedMilliseconds { get; set; }
        public int StatusFinal { get; set; }
        public List<ProcesarItemResult> Items { get; set; } = [];
    }

    public class ProcesarItemResult
    {
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Cantidad { get; set; }
        public double StockAnterior { get; set; }
        public double StockNuevo { get; set; }
        public bool Ok { get; set; }
        public string Error { get; set; } = "";
        public bool YaProcesado { get; set; }
    }
}