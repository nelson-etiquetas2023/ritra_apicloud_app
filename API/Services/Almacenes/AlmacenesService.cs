using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Almacenes
{
    public class AlmacenesService(ApplicationDbContext context) : IAlmacenesService
    {
        private static string FormatAlmacenCode(int number)
        {
            return $"ALM-{number:D4}";
        }

        private async Task<int> GetMaxAlmacenCodeNumberAsync()
        {
            var codigos = await context.Almacenes
                .Where(a => a.almacen_code != null && a.almacen_code.StartsWith("ALM-"))
                .Select(a => a.almacen_code)
                .ToListAsync();

            var max = 0;
            foreach (var codigo in codigos)
            {
                if (codigo.Length == 8 && int.TryParse(codigo.AsSpan(4), out var number) && number > max)
                    max = number;
            }
            return max;
        }

        public async Task<string> GetNextNumAsync()
        {
            var max = await GetMaxAlmacenCodeNumberAsync();
            return FormatAlmacenCode(max + 1);
        }

        public async Task<List<Almacen>> GetAllAsync()
        {
            return await context.Almacenes
                .OrderBy(a => a.almacen_name)
                .ToListAsync();
        }

        public async Task<Almacen?> GetByIdAsync(int id)
        {
            return await context.Almacenes.FirstOrDefaultAsync(a => a.almacen_id == id);
        }

        public async Task<Almacen?> CreateAsync(Almacen almacen)
        {
            if (almacen == null || string.IsNullOrWhiteSpace(almacen.almacen_name)) return null;

            almacen.almacen_name = almacen.almacen_name.Trim();
            almacen.descripcion = almacen.descripcion?.Trim() ?? string.Empty;
            almacen.almacen_id = 0;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                almacen.almacen_code = FormatAlmacenCode(await GetMaxAlmacenCodeNumberAsync() + 1);
                context.Almacenes.Add(almacen);
                try
                {
                    await context.SaveChangesAsync();
                    return almacen;
                }
                catch (DbUpdateException)
                {
                    context.Entry(almacen).State = EntityState.Detached;
                }
            }

            return null;
        }

        public async Task<Almacen?> UpdateAsync(int id, Almacen almacen)
        {
            if (almacen == null || string.IsNullOrWhiteSpace(almacen.almacen_name)) return null;

            var existing = await context.Almacenes.FirstOrDefaultAsync(a => a.almacen_id == id);
            if (existing == null) return null;

            existing.almacen_name = almacen.almacen_name.Trim();
            existing.descripcion = almacen.descripcion?.Trim() ?? string.Empty;

            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var almacen = await context.Almacenes.FirstOrDefaultAsync(a => a.almacen_id == id);
            if (almacen == null) return false;

            context.Almacenes.Remove(almacen);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
