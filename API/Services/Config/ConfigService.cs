using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Config
{
    public class ConfigService : IConfigService
    {
        readonly ApplicationDbContext context;

        public ConfigService(ApplicationDbContext context)
        {
            this.context = context; 
        }
        public async Task<List<Parameter>> LoadConfigurationAsync()
        {
            var data = await context.Parametros.ToListAsync();
            return data;

        }

        public async Task<DocumentSettings?> UpdateDocumntSetting(string filter, DocumentSettings setting)
        {
            if (setting == null) return null;
            var updated = context.Parametros.Where(x => x.Module == filter);
            if (updated == null) return null;
            updated.FirstOrDefault()!.Value1 = setting.Consec.ToString();
            updated.FirstOrDefault()!.Value2 = setting.Prefijo.ToString();
            updated.FirstOrDefault()!.Value3 = setting.useSeparator.ToString();
            updated.FirstOrDefault()!.Value4 = setting.usePref.ToString();
            updated.FirstOrDefault()!.Value5 = setting.CharacterSeparator.ToString();

            await context.SaveChangesAsync();
            return setting;
        }
    }
}
