using Shared.Dtos;

namespace API.Services.Config
{
    public interface IConfigService
    {
        Task<List<Parameter>> LoadConfigurationAsync();
        Task<DocumentSettings?> UpdateDocumntSetting(string filter, DocumentSettings setting);
    }
}
