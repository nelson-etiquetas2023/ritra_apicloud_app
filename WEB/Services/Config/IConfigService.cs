using Shared.Dtos;

namespace WEB.Services.Config
{
    public interface IConfigService
    {
        Task<List<Parameter>> LoadDataConfig();
        Task<bool> UpdateDocumentSettings(string filter, DocumentSettings setting);
    }
}
