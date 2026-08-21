using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Vendedores
{
    public class VendedoresService(ApplicationDbContext context) : IVendedoresService
    {
        private static string FormatVendedorCode(int number)
        {
            return $"VEN-{number:D4}";
        }

        private async Task<int> GetMaxVendedorCodeNumberAsync()
        {
            var codigos = await context.Vendedores
                .Where(v => v.vendedor_code != null && v.vendedor_code.StartsWith("VEN-"))
                .Select(v => v.vendedor_code)
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
            var max = await GetMaxVendedorCodeNumberAsync();
            return FormatVendedorCode(max + 1);
        }

        public async Task<List<Vendedor>> GetAllAsync()
        {
            return await context.Vendedores
                .OrderBy(v => v.vendedor_name)
                .ToListAsync();
        }

        public async Task<Vendedor?> GetByIdAsync(int id)
        {
            return await context.Vendedores.FirstOrDefaultAsync(v => v.vendedor_id == id);
        }

        public async Task<Vendedor?> CreateAsync(Vendedor vendedor)
        {
            if (vendedor == null || string.IsNullOrWhiteSpace(vendedor.vendedor_name)) return null;

            vendedor.vendedor_name = vendedor.vendedor_name.Trim();
            vendedor.telefono = vendedor.telefono?.Trim() ?? string.Empty;
            vendedor.email = vendedor.email?.Trim() ?? string.Empty;
            vendedor.vendedor_id = 0;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                vendedor.vendedor_code = FormatVendedorCode(await GetMaxVendedorCodeNumberAsync() + 1);
                context.Vendedores.Add(vendedor);
                try
                {
                    await context.SaveChangesAsync();
                    return vendedor;
                }
                catch (DbUpdateException)
                {
                    context.Entry(vendedor).State = EntityState.Detached;
                }
            }

            return null;
        }

        public async Task<Vendedor?> UpdateAsync(int id, Vendedor vendedor)
        {
            if (vendedor == null || string.IsNullOrWhiteSpace(vendedor.vendedor_name)) return null;

            var existing = await context.Vendedores.FirstOrDefaultAsync(v => v.vendedor_id == id);
            if (existing == null) return null;

            existing.vendedor_name = vendedor.vendedor_name.Trim();
            existing.telefono = vendedor.telefono?.Trim() ?? string.Empty;
            existing.email = vendedor.email?.Trim() ?? string.Empty;

            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var vendedor = await context.Vendedores.FirstOrDefaultAsync(v => v.vendedor_id == id);
            if (vendedor == null) return false;

            context.Vendedores.Remove(vendedor);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
