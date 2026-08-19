namespace WEB.Services.Inventario
{
    public interface IInventarioService
    {
        Task<Shared.Dtos.Compras.ProcesarOrdenResult?> ProcesarOrdenAsync(string numero);
    }
}