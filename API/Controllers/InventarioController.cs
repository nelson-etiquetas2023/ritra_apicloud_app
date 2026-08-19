using API.Services.Inventario;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController(IInventarioService inventario) : ControllerBase
    {
        private readonly IInventarioService inventario = inventario;

        #region RECEIVE-COMPRAS

        [HttpPost("process-compra/{numero}")]
        public async Task<IActionResult> ProcessCompra(string numero)
        {
            var result = await inventario.ProcesarCompraAsync(numero);
            return Ok(result);
        }

        #endregion

        #region PICKING

        #endregion

        #region PACKING

        #endregion

        #region TRANSFERENCIAS

        #endregion

        #region CONTEO-INVENTARIO

        #endregion

        #region FACTURACION

        #endregion
    }
}