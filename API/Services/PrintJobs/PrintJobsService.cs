using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.PrintJobs
{
    public class PrintJobsService(ApplicationDbContext context) : IPrintJobsService
    {
        private async Task<Enterprise?> ResolveEnterpriseAsync(int? requestedId, int? templateEnterpriseId)
        {
            if (requestedId.HasValue)
            {
                var byRequest = await context.Enterprises
                    .FirstOrDefaultAsync(e => e.enterprise_id == requestedId.Value);
                if (byRequest != null) return byRequest;
            }
            if (templateEnterpriseId.HasValue)
            {
                var byTemplate = await context.Enterprises
                    .FirstOrDefaultAsync(e => e.enterprise_id == templateEnterpriseId.Value);
                if (byTemplate != null) return byTemplate;
            }
            return await context.Enterprises.OrderBy(e => e.enterprise_id).FirstOrDefaultAsync();
        }
        public async Task<List<PrintJob>> GetPendingAsync()
        {
            return await context.PrintJobs
                .Where(j => j.Status == PrintJobStatus.Pending || j.Status == PrintJobStatus.Printing)
                .OrderBy(j => j.Orden)
                .ThenBy(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<PrintJob?> GetByIdAsync(int id)
        {
            return await context.PrintJobs.FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<(PrintJob? Job, string? Error)> EnqueueAsync(PrintJobEnqueueRequest request)
        {
            if (request == null) return (null, "Datos inválidos.");
            if (request.Copies < 1) request.Copies = 1;
            if (request.Copies > 1000) request.Copies = 1000;

            var template = await context.LabelTemplates
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive);
            if (template == null) return (null, $"Plantilla {request.TemplateId} no encontrada.");

            var printer = await context.Printers
                .FirstOrDefaultAsync(p => p.Id == request.PrinterId && p.IsActive);
            if (printer == null) return (null, $"Impresora {request.PrinterId} no encontrada.");
            if (!PrinterConnection.EsRed(printer.Connection))
                return (null, $"La impresora {printer.Name} es {printer.Connection}: imprime desde el diálogo con destino local (USB/serie/paralelo/Bluetooth).");

            Dictionary<string, string> values = LabelGenerator.SampleValues();
            string snapshot = "";
            if (request.ProductId.HasValue)
            {
                var product = await context.Productos
                    .FirstOrDefaultAsync(p => p.Product_id == request.ProductId.Value);
                if (product == null) return (null, $"Producto {request.ProductId} no encontrado.");
                foreach (var kv in LabelGenerator.ProductValues(product)) values[kv.Key] = kv.Value;
                snapshot = LabelGenerator.Snapshot(product);
            }

            var enterprise = await ResolveEnterpriseAsync(request.EnterpriseId, template.EnterpriseId);
            if (enterprise != null)
                foreach (var kv in LabelGenerator.EnterpriseValues(enterprise)) values[kv.Key] = kv.Value;

            string zpl;
            try
            {
                zpl = LabelGenerator.BuildZpl(template, values, request.Copies);
            }
            catch (InvalidOperationException ex)
            {
                return (null, ex.Message);
            }

            var maxOrden = await context.PrintJobs
                .Where(j => j.Status == PrintJobStatus.Pending)
                .MaxAsync(j => (int?)j.Orden) ?? 0;

            var job = new PrintJob
            {
                TemplateId = template.Id,
                PrinterId = printer.Id,
                ProductId = request.ProductId,
                ProductSnapshotJson = snapshot,
                ZplFinal = zpl,
                Copies = request.Copies,
                Status = PrintJobStatus.Pending,
                Orden = maxOrden + 1,
                CreatedBy = request.CreatedBy?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            context.PrintJobs.Add(job);
            await context.SaveChangesAsync();
            return (job, null);
        }

        public async Task<bool> ReorderAsync(List<PrintJobReorderItem> items)
        {
            if (items == null || items.Count == 0) return false;

            var ids = items.Select(i => i.Id).ToList();
            var jobs = await context.PrintJobs
                .Where(j => ids.Contains(j.Id) && j.Status == PrintJobStatus.Pending)
                .ToListAsync();

            foreach (var item in items)
            {
                var job = jobs.FirstOrDefault(j => j.Id == item.Id);
                if (job != null) job.Orden = item.Orden;
            }

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAsync(int id)
        {
            var job = await context.PrintJobs.FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return false;
            if (job.Status is PrintJobStatus.Done or PrintJobStatus.Cancelled) return false;

            job.Status = PrintJobStatus.Cancelled;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<(PrintJob? Job, string? Error)> RetryAsync(int id)
        {
            var job = await context.PrintJobs.FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return (null, $"Trabajo {id} no encontrado.");
            if (job.Status != PrintJobStatus.Failed && job.Status != PrintJobStatus.Cancelled)
                return (null, "Solo se reintentan trabajos fallidos o cancelados.");

            var maxOrden = await context.PrintJobs
                .Where(j => j.Status == PrintJobStatus.Pending)
                .MaxAsync(j => (int?)j.Orden) ?? 0;

            job.Status = PrintJobStatus.Pending;
            job.Error = string.Empty;
            job.Orden = maxOrden + 1;
            await context.SaveChangesAsync();
            return (job, null);
        }

        public async Task<(string Zpl, string? Error)> BuildPreviewZplAsync(int? templateId, string? zpl, int? productId, int copies = 1)
        {
            LabelTemplate? template = null;
            if (templateId.HasValue)
            {
                template = await context.LabelTemplates.FirstOrDefaultAsync(t => t.Id == templateId.Value);
                if (template == null) return ("", $"Plantilla {templateId} no encontrada.");
            }

            var source = template ?? new LabelTemplate { ZplTemplate = zpl ?? "", WidthIn = 2, HeightIn = 1, Dpi = 203 };

            Dictionary<string, string> values = LabelGenerator.SampleValues();
            if (productId.HasValue)
            {
                var product = await context.Productos.FirstOrDefaultAsync(p => p.Product_id == productId.Value);
                if (product == null) return ("", $"Producto {productId} no encontrado.");
                foreach (var kv in LabelGenerator.ProductValues(product)) values[kv.Key] = kv.Value;
            }

            var enterprise = await ResolveEnterpriseAsync(null, template?.EnterpriseId);
            if (enterprise != null)
                foreach (var kv in LabelGenerator.EnterpriseValues(enterprise)) values[kv.Key] = kv.Value;

            try
            {
                return (LabelGenerator.BuildZpl(source, values, copies < 1 ? 1 : copies), null);
            }
            catch (InvalidOperationException ex)
            {
                return ("", ex.Message);
            }
        }
    }
}
