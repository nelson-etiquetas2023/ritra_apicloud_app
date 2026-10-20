using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Turnos;
using Shared.Dtos.Ventas;

namespace API.Services.Turnos
{
    public class TurnosService(ApplicationDbContext context) : ITurnosService
    {
        public async Task<TurnoCaja?> GetAbiertoAsync(string caja)
        {
            if (string.IsNullOrWhiteSpace(caja)) caja = "Caja 1";
            return await context.TurnosCaja
                .FirstOrDefaultAsync(t => t.Caja == caja.Trim() && t.Status == TurnoStatus.Abierto);
        }

        public async Task<List<TurnoCaja>> GetAllAsync()
        {
            return await context.TurnosCaja.OrderByDescending(t => t.Id).ToListAsync();
        }

        public async Task<TurnoCaja?> GetByIdAsync(int id)
        {
            return await context.TurnosCaja.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<TurnoDenominacion>> GetConteosAsync(int turnoId)
        {
            return await context.TurnoDenominaciones
                .Where(d => d.TurnoId == turnoId)
                .OrderByDescending(d => d.Denominacion)
                .ToListAsync();
        }

        public async Task<List<TurnoMovimiento>> GetMovimientosAsync(int turnoId)
        {
            return await context.TurnoMovimientos
                .Where(m => m.TurnoId == turnoId)
                .OrderBy(m => m.Fecha)
                .ToListAsync();
        }

        public async Task<TurnoSaveResult> AbrirAsync(AbrirTurnoRequest request)
        {
            var result = new TurnoSaveResult();

            var caja = string.IsNullOrWhiteSpace(request.Caja) ? "Caja 1" : request.Caja.Trim();
            if (string.IsNullOrWhiteSpace(request.Cajero))
            {
                result.Success = false;
                result.Message = "El cajero es obligatorio para abrir el turno.";
                return result;
            }
            if (request.FondoInicial < 0)
            {
                result.Success = false;
                result.Message = "El fondo inicial no puede ser negativo.";
                return result;
            }

            var abierto = await GetAbiertoAsync(caja);
            if (abierto != null)
            {
                result.Success = false;
                result.Message = $"La {caja} ya tiene un turno abierto ({abierto.Cajero}, {abierto.FechaApertura:dd/MM/yyyy HH:mm}). Ciérralo antes de abrir otro.";
                return result;
            }

            var turno = new TurnoCaja
            {
                Caja = caja,
                Cajero = request.Cajero.Trim(),
                FechaApertura = DateTime.Now,
                FondoInicial = request.FondoInicial,
                Status = TurnoStatus.Abierto
            };
            context.TurnosCaja.Add(turno);
            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Turno abierto en {caja} por {turno.Cajero} con fondo {turno.FondoInicial:N2}.";
            result.Data = turno;
            return result;
        }

        public async Task<TurnoSaveResult> AgregarMovimientoAsync(TurnoMovimientoRequest request)
        {
            var result = new TurnoSaveResult();

            var turno = await context.TurnosCaja.FirstOrDefaultAsync(t => t.Id == request.TurnoId);
            if (turno == null)
            {
                result.Success = false;
                result.Message = $"El turno {request.TurnoId} no existe.";
                return result;
            }
            if (turno.Status != TurnoStatus.Abierto)
            {
                result.Success = false;
                result.Message = "El turno ya está cerrado y no admite movimientos.";
                return result;
            }

            var tipo = request.Tipo?.Trim();
            if (tipo != TurnoMovimientoTipo.Entrada && tipo != TurnoMovimientoTipo.Salida)
            {
                result.Success = false;
                result.Message = "El tipo debe ser Entrada o Salida.";
                return result;
            }
            if (request.Monto <= 0)
            {
                result.Success = false;
                result.Message = "El monto debe ser mayor a cero.";
                return result;
            }
            if (string.IsNullOrWhiteSpace(request.Motivo))
            {
                result.Success = false;
                result.Message = "El motivo es obligatorio.";
                return result;
            }

            context.TurnoMovimientos.Add(new TurnoMovimiento
            {
                TurnoId = turno.Id,
                Fecha = DateTime.Now,
                Tipo = tipo,
                Monto = request.Monto,
                Motivo = request.Motivo.Trim()
            });
            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"{tipo} de {request.Monto:N2} registrada.";
            result.Data = turno;
            return result;
        }

        public async Task<TurnoResumen?> GetResumenAsync(int turnoId)
        {
            var turno = await context.TurnosCaja.FirstOrDefaultAsync(t => t.Id == turnoId);
            if (turno == null) return null;

            var ventas = await context.PedidoVenta
                .Include(p => p.Pagos)
                .Where(p => p.TurnoId == turnoId && p.Status == 4)
                .ToListAsync();

            var anuladas = await context.PedidoVenta
                .Where(p => p.TurnoId == turnoId && p.Status == 6)
                .ToListAsync();

            var entradas = await context.TurnoMovimientos
                .Where(m => m.TurnoId == turnoId && m.Tipo == TurnoMovimientoTipo.Entrada)
                .SumAsync(m => (decimal?)m.Monto) ?? 0;

            var salidas = await context.TurnoMovimientos
                .Where(m => m.TurnoId == turnoId && m.Tipo == TurnoMovimientoTipo.Salida)
                .SumAsync(m => (decimal?)m.Monto) ?? 0;

            var ventasEfectivo = ventas.Sum(p => p.Pagos.Count > 0
                ? p.Pagos.Where(g => g.Metodo == MetodoPago.Efectivo).Sum(g => g.Monto)
                : p.Total);
            var ventasElectronico = ventas
                .Where(p => p.Pagos.Count > 0)
                .Sum(p => p.Pagos.Where(g => g.Metodo != MetodoPago.Efectivo).Sum(g => g.Monto));

            return new TurnoResumen
            {
                TurnoId = turnoId,
                FondoInicial = turno.FondoInicial,
                VentasProcesadas = ventas.Count,
                VentasEfectivo = ventasEfectivo,
                VentasElectronico = ventasElectronico,
                FacturasAnuladas = anuladas.Count,
                MontoAnulado = anuladas.Sum(p => p.Total),
                VentaNeta = ventasEfectivo + ventasElectronico,
                Entradas = entradas,
                Salidas = salidas,
                Esperado = turno.FondoInicial + ventasEfectivo + entradas - salidas
            };
        }

        public async Task<TurnoCierreResult> CerrarAsync(int turnoId, CerrarTurnoRequest request)
        {
            var result = new TurnoCierreResult();

            var turno = await context.TurnosCaja.FirstOrDefaultAsync(t => t.Id == turnoId);
            if (turno == null)
            {
                result.Success = false;
                result.Message = $"El turno {turnoId} no existe.";
                return result;
            }
            if (turno.Status != TurnoStatus.Abierto)
            {
                result.Success = false;
                result.Message = "El turno ya está cerrado.";
                return result;
            }

            var resumen = await GetResumenAsync(turnoId);
            if (resumen == null)
            {
                result.Success = false;
                result.Message = $"El turno {turnoId} no existe.";
                return result;
            }

            var conteos = (request.Conteos ?? [])
                .Where(c => c.Cantidad > 0)
                .GroupBy(c => c.Denominacion)
                .Select(g => new TurnoDenominacion
                {
                    TurnoId = turnoId,
                    Denominacion = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad)
                })
                .ToList();

            var contado = conteos.Sum(c => c.Denominacion * c.Cantidad);
            var diferencia = contado - resumen.Esperado;

            if (diferencia != 0 && string.IsNullOrWhiteSpace(request.Observacion))
            {
                result.Success = false;
                result.Message = $"Hay una diferencia de {diferencia:N2}. Indica el motivo en la observación para cerrar.";
                return result;
            }

            turno.FechaCierre = DateTime.Now;
            turno.Status = TurnoStatus.Cerrado;
            turno.Observacion = request.Observacion?.Trim() ?? "";
            turno.Esperado = resumen.Esperado;
            turno.Contado = contado;
            turno.Diferencia = diferencia;
            if (!string.IsNullOrWhiteSpace(request.CerradoPor))
                turno.Observacion = string.IsNullOrWhiteSpace(turno.Observacion)
                    ? $"Cerrado por {request.CerradoPor.Trim()}."
                    : $"{turno.Observacion} (Cerrado por {request.CerradoPor.Trim()})";

            context.TurnoDenominaciones.AddRange(conteos);
            await context.SaveChangesAsync();

            result.Success = true;
            result.Message = diferencia == 0
                ? $"Turno cerrado cuadrado. Contado {contado:N2}."
                : $"Turno cerrado con diferencia de {diferencia:N2} ({(diferencia > 0 ? "sobrante" : "faltante")}).";
            result.Turno = turno;
            result.Conteos = conteos;
            return result;
        }
    }
}
