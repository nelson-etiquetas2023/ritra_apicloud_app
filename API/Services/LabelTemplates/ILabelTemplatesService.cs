using Shared.Dtos;

namespace API.Services.LabelTemplates
{
    public interface ILabelTemplatesService
    {
        Task<List<LabelTemplate>> GetAllAsync();
        Task<LabelTemplate?> GetByIdAsync(int id);
        Task<LabelTemplate?> CreateAsync(LabelTemplate template);
        Task<LabelTemplate?> UpdateAsync(int id, LabelTemplate template);
        Task<bool> DeleteAsync(int id);
        Task<string> GetNextNumAsync();
        Task<LabelTemplate?> DuplicateAsync(int id);
        Task<LabelTemplate?> GetDefaultAsync();
        Task<(LabelTemplate? Template, string? Error)> SetDefaultAsync(int id);
    }
}
