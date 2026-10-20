using Shared.Dtos.Turnos;
using System.Text;
using System.Text.Json;

namespace WEB.Services.Turnos
{
    public interface ITurnosService
    {
        Task<TurnoCaja?> GetAbiertoAsync(string caja = "Caja 1");
        Task<List<TurnoCaja>> GetAllAsync();
        Task<TurnoCaja?> GetByIdAsync(int id);
        Task<List<TurnoDenominacion>> GetConteosAsync(int turnoId);
        Task<List<TurnoMovimiento>> GetMovimientosAsync(int turnoId);
        Task<TurnoResumen?> GetResumenAsync(int turnoId);
        Task<TurnoSaveResult> AbrirAsync(string caja, string cajero, decimal fondoInicial);
        Task<TurnoSaveResult> AgregarMovimientoAsync(int turnoId, string tipo, decimal monto, string motivo);
        Task<TurnoCierreResult?> CerrarAsync(int turnoId, List<ConteoDenominacion> conteos, string observacion, string cerradoPor);
    }

    public class TurnosService(IHttpClientFactory httpFactory) : ITurnosService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        public async Task<TurnoCaja?> GetAbiertoAsync(string caja = "Caja 1")
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync($"api/turnos/getabierto?caja={Uri.EscapeDataString(caja)}");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return null;
                return await JsonSerializer.DeserializeAsync<TurnoCaja>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions);
            }
            catch { return null; }
        }

        public async Task<List<TurnoCaja>> GetAllAsync()
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync("api/turnos/get");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return [];
                return await JsonSerializer.DeserializeAsync<List<TurnoCaja>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
            }
            catch { return []; }
        }

        public async Task<TurnoCaja?> GetByIdAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync($"api/turnos/getbyid/{id}");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return null;
                return await JsonSerializer.DeserializeAsync<TurnoCaja>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions);
            }
            catch { return null; }
        }

        public async Task<List<TurnoDenominacion>> GetConteosAsync(int turnoId)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync($"api/turnos/conteos/{turnoId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return [];
                return await JsonSerializer.DeserializeAsync<List<TurnoDenominacion>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
            }
            catch { return []; }
        }

        public async Task<List<TurnoMovimiento>> GetMovimientosAsync(int turnoId)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync($"api/turnos/movimientos/{turnoId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return [];
                return await JsonSerializer.DeserializeAsync<List<TurnoMovimiento>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
            }
            catch { return []; }
        }

        public async Task<TurnoResumen?> GetResumenAsync(int turnoId)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync($"api/turnos/resumen/{turnoId}");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return null;
                return await JsonSerializer.DeserializeAsync<TurnoResumen>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions);
            }
            catch { return null; }
        }

        public async Task<TurnoSaveResult> AbrirAsync(string caja, string cajero, decimal fondoInicial)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var req = new AbrirTurnoRequest { Caja = caja, Cajero = cajero, FondoInicial = fondoInicial };
                var content = new StringContent(JsonSerializer.Serialize(req, jsonOptions), Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync("api/turnos/abrir", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new TurnoSaveResult { Success = false, Message = "El servidor no devolvió respuesta." };
                return await JsonSerializer.DeserializeAsync<TurnoSaveResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions)
                    ?? new TurnoSaveResult { Success = false, Message = "No se pudo abrir el turno." };
            }
            catch (Exception ex)
            {
                return new TurnoSaveResult { Success = false, Message = $"No se pudo conectar: {ex.Message}" };
            }
        }

        public async Task<TurnoSaveResult> AgregarMovimientoAsync(int turnoId, string tipo, decimal monto, string motivo)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var req = new TurnoMovimientoRequest { TurnoId = turnoId, Tipo = tipo, Monto = monto, Motivo = motivo };
                var content = new StringContent(JsonSerializer.Serialize(req, jsonOptions), Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync("api/turnos/movimiento", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new TurnoSaveResult { Success = false, Message = "El servidor no devolvió respuesta." };
                return await JsonSerializer.DeserializeAsync<TurnoSaveResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions)
                    ?? new TurnoSaveResult { Success = false, Message = "No se pudo registrar el movimiento." };
            }
            catch (Exception ex)
            {
                return new TurnoSaveResult { Success = false, Message = $"No se pudo conectar: {ex.Message}" };
            }
        }

        public async Task<TurnoCierreResult?> CerrarAsync(int turnoId, List<ConteoDenominacion> conteos, string observacion, string cerradoPor)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var req = new CerrarTurnoRequest { Conteos = conteos, Observacion = observacion, CerradoPor = cerradoPor };
                var content = new StringContent(JsonSerializer.Serialize(req, jsonOptions), Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync($"api/turnos/cerrar/{turnoId}", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson)) return null;
                return await JsonSerializer.DeserializeAsync<TurnoCierreResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions);
            }
            catch { return null; }
        }
    }
}
