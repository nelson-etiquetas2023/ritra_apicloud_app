using Shared.Dtos.Turnos;

namespace API.Services.Turnos
{
    public interface ITurnosService
    {
        Task<TurnoCaja?> GetAbiertoAsync(string caja);
        Task<List<TurnoCaja>> GetAllAsync();
        Task<TurnoCaja?> GetByIdAsync(int id);
        Task<List<TurnoDenominacion>> GetConteosAsync(int turnoId);
        Task<List<TurnoMovimiento>> GetMovimientosAsync(int turnoId);
        Task<TurnoSaveResult> AbrirAsync(AbrirTurnoRequest request);
        Task<TurnoSaveResult> AgregarMovimientoAsync(TurnoMovimientoRequest request);
        Task<TurnoResumen?> GetResumenAsync(int turnoId);
        Task<TurnoCierreResult> CerrarAsync(int turnoId, CerrarTurnoRequest request);
    }
}
