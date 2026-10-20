using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using System.Globalization;

namespace API.Services.LabelTemplates
{
    public class LabelTemplatesService(ApplicationDbContext context, IHttpClientFactory httpFactory) : ILabelTemplatesService
    {
        private static string FormatCode(int number) => $"LBL-{number:D4}";

        private async Task<int> GetMaxCodeNumberAsync()
        {
            var codigos = await context.LabelTemplates
                .Where(t => t.Codigo != null && t.Codigo.StartsWith("LBL-"))
                .Select(t => t.Codigo)
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
            return FormatCode(await GetMaxCodeNumberAsync() + 1);
        }

        public async Task<List<LabelTemplate>> GetAllAsync()
        {
            return await context.LabelTemplates
                .OrderBy(t => t.Nombre)
                .ToListAsync();
        }

        public async Task<LabelTemplate?> GetByIdAsync(int id)
        {
            return await context.LabelTemplates.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<LabelTemplate?> CreateAsync(LabelTemplate template)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.Nombre)) return null;

            template.Nombre = template.Nombre.Trim();
            template.Categoria = LabelCategories.All.Contains(template.Categoria)
                ? template.Categoria
                : LabelCategories.Producto;
            if (template.WidthIn <= 0) template.WidthIn = 2;
            if (template.HeightIn <= 0) template.HeightIn = 1;
            if (template.Dpi is not (152 or 203 or 300 or 600)) template.Dpi = 203;
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            template.Id = 0;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                template.Codigo = FormatCode(await GetMaxCodeNumberAsync() + 1);
                context.LabelTemplates.Add(template);
                try
                {
                    await context.SaveChangesAsync();
                    await TryRefreshThumbnailAsync(template);
                    return template;
                }
                catch (DbUpdateException)
                {
                    context.Entry(template).State = EntityState.Detached;
                }
            }

            return null;
        }

        private static string Inv(double value) =>
            value.ToString("0.##", CultureInfo.InvariantCulture);

        // Mejor esfuerzo: si Labelary falla (red/límite), la plantilla queda sin miniatura.
        private async Task TryRefreshThumbnailAsync(LabelTemplate template)
        {
            if (string.IsNullOrWhiteSpace(template.ZplTemplate)) return;

            try
            {
                var client = httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                var url = $"https://api.labelary.com/v1/printers/12dpmm/labels/" +
                          $"{Inv(template.WidthIn)}x{Inv(template.HeightIn)}/0/";
                var content = new StringContent(template.ZplTemplate, System.Text.Encoding.UTF8,
                    "application/x-www-form-urlencoded");
                var response = await client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) return;
                var png = await response.Content.ReadAsByteArrayAsync();
                template.ThumbnailBase64 = $"data:image/png;base64,{Convert.ToBase64String(png)}";
                template.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
            catch
            {
                // Sin miniatura; el usuario puede regenerarla desde Vista previa.
            }
        }

        public async Task<LabelTemplate?> UpdateAsync(int id, LabelTemplate template)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.Nombre)) return null;

            var existing = await context.LabelTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (existing == null) return null;

            var zplCambio = existing.ZplTemplate != (template.ZplTemplate ?? string.Empty)
                || existing.WidthIn != template.WidthIn
                || existing.HeightIn != template.HeightIn
                || existing.Dpi != template.Dpi;

            existing.Nombre = template.Nombre.Trim();
            existing.Categoria = LabelCategories.All.Contains(template.Categoria)
                ? template.Categoria
                : LabelCategories.Producto;
            if (template.WidthIn > 0) existing.WidthIn = template.WidthIn;
            if (template.HeightIn > 0) existing.HeightIn = template.HeightIn;
            if (template.Dpi is 152 or 203 or 300 or 600) existing.Dpi = template.Dpi;
            existing.DesignJson = template.DesignJson ?? string.Empty;
            existing.ZplTemplate = template.ZplTemplate ?? string.Empty;
            existing.ThumbnailBase64 = template.ThumbnailBase64;
            existing.PrinterId = template.PrinterId;
            existing.EnterpriseId = template.EnterpriseId;
            existing.IsActive = template.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            if (zplCambio || string.IsNullOrWhiteSpace(existing.ThumbnailBase64))
                await TryRefreshThumbnailAsync(existing);

            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var template = await context.LabelTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (template == null) return false;

            var enCola = await context.PrintJobs.AnyAsync(j =>
                j.TemplateId == id &&
                (j.Status == PrintJobStatus.Pending || j.Status == PrintJobStatus.Printing));
            if (enCola) return false;

            context.LabelTemplates.Remove(template);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<LabelTemplate?> DuplicateAsync(int id)
        {
            var src = await context.LabelTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (src == null) return null;

            var clone = new LabelTemplate
            {
                Nombre = src.Nombre + " (copia)",
                Categoria = src.Categoria,
                WidthIn = src.WidthIn,
                HeightIn = src.HeightIn,
                Dpi = src.Dpi,
                DesignJson = src.DesignJson,
                ZplTemplate = src.ZplTemplate,
                ThumbnailBase64 = src.ThumbnailBase64,
                PrinterId = src.PrinterId,
                EnterpriseId = src.EnterpriseId,
                IsActive = src.IsActive
            };
            return await CreateAsync(clone);
        }

        private const string DefaultParamName = "EtiquetaDefaultTemplateId";
        private const string DefaultParamModule = "Etiquetas";

        public async Task<LabelTemplate?> GetDefaultAsync()
        {
            var param = await context.Parametros
                .FirstOrDefaultAsync(p => p.Module == DefaultParamModule && p.Name == DefaultParamName);
            if (param == null || !int.TryParse(param.Value1, out var id)) return null;
            return await context.LabelTemplates
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
        }

        public async Task<(LabelTemplate? Template, string? Error)> SetDefaultAsync(int id)
        {
            var template = await context.LabelTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (template == null) return (null, $"Plantilla {id} no encontrada.");
            if (!template.IsActive) return (null, "La plantilla está inactiva.");
            if (template.Categoria != LabelCategories.Producto)
                return (null, $"Solo plantillas de categoría Producto (la {template.Codigo} es {template.Categoria}).");

            var param = await context.Parametros
                .FirstOrDefaultAsync(p => p.Module == DefaultParamModule && p.Name == DefaultParamName);
            if (param == null)
            {
                param = new Parameter
                {
                    Module = DefaultParamModule,
                    Name = DefaultParamName,
                    Value1 = id.ToString(),
                    Value2 = "", Value3 = "", Value4 = "", Value5 = "",
                    Value6 = "", Value7 = "", Value8 = "", Value9 = "", Value10 = ""
                };
                context.Parametros.Add(param);
            }
            else
            {
                param.Value1 = id.ToString();
            }

            await context.SaveChangesAsync();
            return (template, null);
        }
    }
}
