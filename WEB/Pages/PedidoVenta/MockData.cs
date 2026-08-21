namespace WEB.Pages.PedidoVenta
{
    public class ClienteMock
    {
        public int Cliente_Id { get; set; }
        public string Cliente_Nombre { get; set; } = "";
        public string Cliente_RNC { get; set; } = "";
        public string Direccion { get; set; } = "";
    }

    public class ProductoMock
    {
        public string Codebar { get; set; } = "";
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Costo { get; set; }
        public decimal Precio { get; set; }
        public decimal Stock { get; set; }
    }
}
